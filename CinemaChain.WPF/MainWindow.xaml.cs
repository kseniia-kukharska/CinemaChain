using CinemaChain.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Data; // Для Native SQL
using System.Windows;
using System.Windows.Controls;

namespace CinemaChain.WPF
{
    public partial class MainWindow : Window
    {
        private CinemaDbContext _context;

        // Variables for Data
        private Cinema? _selectedCinema = null;
        private Hall? _selectedHall = null;

        // Variables for Pagination
        private int _currentPage = 1;
        private int _pageSize = 10; // Змінено з 3 на 10, як ви просили
        private int _totalMovies = 0;

        public MainWindow()
        {
            InitializeComponent();
            _context = new CinemaDbContext();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadData();
            LoadMovies();       // Init pagination tab
            LoadSession();     // Init transaction tab
            RefreshAnalytics(); // Init analytics tab
        }

        private void FilterCinemas(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                CinemasGrid.ItemsSource = _context.Cinemas.ToList();
            }
            else
            {
                // Фільтрація по місту або адресі
                CinemasGrid.ItemsSource = _context.Cinemas
                   .Where(c => c.City.ToLower().Contains(filterText.ToLower()) ||
                               c.Address.ToLower().Contains(filterText.ToLower()))
                   .ToList();
            }
        }

        // Приклад обробника події (потрібно додати TextBox в XAML і прив'язати цю подію)
        private void SearchCinema_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                FilterCinemas(textBox.Text);
            }
        }

        private void LoadData()
        {
            // Cinemas & Halls Tab
            var cinemas = _context.Cinemas.ToList();
            CinemasGrid.ItemsSource = cinemas;
            CinemaComboBox.ItemsSource = cinemas;
            HallsGrid.ItemsSource = _context.Halls.Include(h => h.Cinema).ToList();
        }

        // ==========================================
        // 1. MOVIES TAB (PAGINATION & SEARCH)
        // ==========================================
        private void LoadMovies()
        {
            var query = _context.Movies.AsQueryable();

            // Search Logic
            string filter = SearchMovieTxt.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(m => m.Title.ToLower().Contains(filter.ToLower()));
            }

            // Pagination Logic
            _totalMovies = query.Count();
            int totalPages = (int)Math.Ceiling((double)_totalMovies / _pageSize);
            if (totalPages < 1) totalPages = 1;

            if (_currentPage > totalPages) _currentPage = totalPages;
            if (_currentPage < 1) _currentPage = 1;

            var items = query.OrderBy(m => m.Title)
                             .Skip((_currentPage - 1) * _pageSize)
                             .Take(_pageSize)
                             .ToList();

            MoviesGrid.ItemsSource = items;
            TxtPageInfo.Text = $"Page {_currentPage} of {totalPages}";

            // Enable/Disable buttons
            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void SearchMovies_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1; // Reset to first page on search
            LoadMovies();
        }

        private void PrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadMovies();
            }
        }

        private void NextPage_Click(object sender, RoutedEventArgs e)
        {
            _currentPage++;
            LoadMovies();
        }

        // ==========================================
        // 2. SALES TAB (TRANSACTIONS)
        // ==========================================

        // Helper class for ComboBox
        public class SessionDisplay { public int SessionId { get; set; } public string DisplayInfo { get; set; } = ""; }

        private void LoadSession()
        {
            // Loading sessions with simple projection for display
            var sessions = _context.Session
                .Include(s => s.Movie)
                .Include(s => s.Hall)
                .Select(s => new SessionDisplay
                {
                    SessionId = s.SessionId,
                    DisplayInfo = $"{s.Movie.Title} ({s.StartTime:g}) - ${s.Price}"
                })
                .ToList();

            SessionCombo.ItemsSource = sessions;
        }

        private void SellTicket_Click(object sender, RoutedEventArgs e)
        {
            if (SessionCombo.SelectedValue == null) { MessageBox.Show("Select a session!"); return; }
            if (!int.TryParse(TxtRow.Text, out int row) || !int.TryParse(TxtSeat.Text, out int seat))
            {
                MessageBox.Show("Invalid Row or Seat number."); return;
            }

            int sessionId = (int)SessionCombo.SelectedValue;

            // === TRANSACTION START ===
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Створення квитка
                var ticket = new Ticket
                {
                    SessionId = sessionId,
                    Row = row,
                    SeatNumber = seat,
                    IsSold = true
                };

                _context.Tickets.Add(ticket);
                _context.SaveChanges(); // This triggers the DB "CheckDoubleBooking" trigger

                transaction.Commit();
                // === TRANSACTION COMMIT ===

                MessageBox.Show("Ticket Sold Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                TxtRow.Clear(); TxtSeat.Clear();

                // Оновити статистику, бо дані змінилися
                RefreshAnalytics();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                // === TRANSACTION ROLLBACK ===

                // Show clean error message
                string msg = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show($"Transaction Failed (Rolled Back):\n{msg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ==========================================
        // 3. ANALYTICS TAB (NATIVE SQL & VIEWS)
        // ==========================================
        private void RefreshAnalytics_Click(object sender, RoutedEventArgs e)
        {
            RefreshAnalytics();
        }

        private void RefreshAnalytics()
        {
            try
            {
                // 1. Call VIEW using Raw SQL
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT * FROM \"View_SessionDetails\"";
                _context.Database.OpenConnection();

                using var reader = cmd.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load(reader);
                AnalyticsGrid.ItemsSource = dataTable.DefaultView;

                // 2. Call SCALAR FUNCTION using Raw SQL
                // Let's get total revenue for the first cinema (ID 10000 usually) or just any sum
                // Тут ми трохи схитруємо і порахуємо загальну суму по всіх проданих квитках через SQL
                using var cmdFunc = _context.Database.GetDbConnection().CreateCommand();
                cmdFunc.CommandText = "SELECT SUM(\"Price\") FROM \"Ticket\" t JOIN \"Session\" s ON t.\"SessionId\" = s.\"SessionId\" WHERE t.\"IsSold\" = true";
                var result = cmdFunc.ExecuteScalar();

                string revenue = result != DBNull.Value ? result.ToString() : "0";
                TxtTotalRevenue.Text = $"Total Revenue: ${revenue}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SQL Error: {ex.Message}");
            }
            finally
            {
                _context.Database.CloseConnection();
            }
        }

        // ==========================================
        // EXISTING LOGIC (CINEMAS & HALLS)
        // ==========================================
        private void CinemasGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CinemasGrid.SelectedItem is Cinema cinema) { _selectedCinema = cinema; CinCityTxt.Text = cinema.City; CinAddressTxt.Text = cinema.Address; CinEmailTxt.Text = cinema.Email; }
        }
        private void AddCinema_Click(object sender, RoutedEventArgs e)
        {
            _context.Cinemas.Add(new Cinema { City = CinCityTxt.Text, Address = CinAddressTxt.Text, Email = CinEmailTxt.Text });
            SaveAndRefresh();
        }
        private void UpdateCinema_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCinema == null) return;
            _selectedCinema.City = CinCityTxt.Text; _selectedCinema.Address = CinAddressTxt.Text; _selectedCinema.Email = CinEmailTxt.Text;
            _context.Cinemas.Update(_selectedCinema);
            SaveAndRefresh();
        }
        private void DeleteCinema_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedCinema != null && MessageBox.Show("Delete?", "Confirm", MessageBoxButton.YesNo) == MessageBoxResult.Yes) { _context.Cinemas.Remove(_selectedCinema); SaveAndRefresh(); }
        }
        private void ClearCinema_Click(object sender, RoutedEventArgs e) { CinCityTxt.Clear(); CinAddressTxt.Clear(); CinEmailTxt.Clear(); _selectedCinema = null; CinemasGrid.SelectedItem = null; }

        private void HallsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HallsGrid.SelectedItem is Hall hall) { _selectedHall = hall; HallNameTxt.Text = hall.Name; HallSeatsTxt.Text = hall.SeatsCount.ToString(); CinemaComboBox.SelectedValue = hall.CinemaId; }
        }
        private void AddHall_Click(object sender, RoutedEventArgs e)
        {
            if (CinemaComboBox.SelectedValue == null) return;
            if (int.TryParse(HallSeatsTxt.Text, out int s)) { _context.Halls.Add(new Hall { Name = HallNameTxt.Text, SeatsCount = s, CinemaId = (int)CinemaComboBox.SelectedValue }); SaveAndRefresh(); }
        }
        private void UpdateHall_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHall != null && CinemaComboBox.SelectedValue != null) { _selectedHall.Name = HallNameTxt.Text; _selectedHall.SeatsCount = int.Parse(HallSeatsTxt.Text); _selectedHall.CinemaId = (int)CinemaComboBox.SelectedValue; _context.Halls.Update(_selectedHall); SaveAndRefresh(); }
        }
        private void DeleteHall_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedHall != null) { _context.Halls.Remove(_selectedHall); SaveAndRefresh(); }
        }
        private void ClearHall_Click(object sender, RoutedEventArgs e) { HallNameTxt.Clear(); HallSeatsTxt.Clear(); CinemaComboBox.SelectedIndex = -1; _selectedHall = null; HallsGrid.SelectedItem = null; }

        private void SaveAndRefresh()
        {
            try
            {
                _context.SaveChanges();
                LoadData();
                ClearCinema_Click(null, null);
                ClearHall_Click(null, null);
            }
            catch (Exception ex) { MessageBox.Show($"Error: {ex.Message}"); }
        }
    }
}