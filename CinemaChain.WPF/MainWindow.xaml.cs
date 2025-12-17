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

        // Змінні для збереження стану вибору
        private Cinema? _selectedCinema = null;
        private Hall? _selectedHall = null;

        // Пагінація
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalMovies = 0;

        public MainWindow()
        {
            InitializeComponent();
            _context = new CinemaDbContext();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LoadData();
                LoadMovies();
                LoadSession();
                RefreshAnalytics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading data: {ex.Message}");
            }
        }

        private void LoadData()
        {
            var cinemas = _context.Cinemas.ToList();

            // Оновлюємо таблицю кінотеатрів
            CinemasGrid.ItemsSource = cinemas;

            // Оновлюємо випадаючий список для РЕДАГУВАННЯ залу
            CinemaComboBox.ItemsSource = cinemas;

            // Оновлюємо випадаючий список для ФІЛЬТРАЦІЇ залів
            HallFilterCombo.ItemsSource = cinemas;

            // За замовчуванням показуємо ВСІ зали
            HallsGrid.ItemsSource = _context.Halls.Include(h => h.Cinema).ToList();
        }

        // ==========================================
        // NEW: HALLS FILTER LOGIC
        // ==========================================
        private void HallFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HallFilterCombo.SelectedItem is Cinema selectedCinema)
            {
                // Фільтруємо таблицю по ID обраного кінотеатру
                HallsGrid.ItemsSource = _context.Halls
                    .Include(h => h.Cinema)
                    .Where(h => h.CinemaId == selectedCinema.CinemaId)
                    .ToList();
            }
            else
            {
                // Якщо нічого не обрано - показуємо все
                HallsGrid.ItemsSource = _context.Halls.Include(h => h.Cinema).ToList();
            }
        }

        private void ClearHallFilter_Click(object sender, RoutedEventArgs e)
        {
            // Скидаємо вибір, що автоматично викличе SelectionChanged і покаже всі зали
            HallFilterCombo.SelectedIndex = -1;
        }


        // ==========================================
        // 1. MOVIES TAB
        // ==========================================
        private void LoadMovies()
        {
            var query = _context.Movies.AsQueryable();

            string filter = SearchMovieTxt.Text.Trim();
            if (!string.IsNullOrEmpty(filter))
            {
                query = query.Where(m => m.Title.ToLower().Contains(filter.ToLower()));
            }

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

            BtnPrevPage.IsEnabled = _currentPage > 1;
            BtnNextPage.IsEnabled = _currentPage < totalPages;
        }

        private void SearchMovies_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            LoadMovies();
        }
        private void PrevPage_Click(object sender, RoutedEventArgs e) { if (_currentPage > 1) { _currentPage--; LoadMovies(); } }
        private void NextPage_Click(object sender, RoutedEventArgs e) { _currentPage++; LoadMovies(); }

        // ==========================================
        // 2. SALES TAB (TRANSACTIONS)
        // ==========================================
        public class SessionDisplay { public int SessionId { get; set; } public string DisplayInfo { get; set; } = ""; }

        private void LoadSession()
        {
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

            using (var localContext = new CinemaDbContext())
            using (var transaction = localContext.Database.BeginTransaction())
            {
                try
                {
                    var ticket = new Ticket
                    {
                        SessionId = sessionId,
                        Row = row,
                        SeatNumber = seat,
                        IsSold = true
                    };

                    localContext.Tickets.Add(ticket);
                    localContext.SaveChanges();

                    transaction.Commit();

                    MessageBox.Show("Ticket Sold Successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    TxtRow.Clear(); TxtSeat.Clear();
                    RefreshAnalytics();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    string msg = ex.InnerException?.Message ?? ex.Message;
                    MessageBox.Show($"Transaction Failed (Rolled Back):\n{msg}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ==========================================
        // 3. ANALYTICS TAB
        // ==========================================
        private void RefreshAnalytics_Click(object sender, RoutedEventArgs e)
        {
            RefreshAnalytics();
        }

        private void RefreshAnalytics()
        {
            try
            {
                using var cmd = _context.Database.GetDbConnection().CreateCommand();
                cmd.CommandText = "SELECT * FROM \"View_SessionDetails\"";
                _context.Database.OpenConnection();

                using var reader = cmd.ExecuteReader();
                var dataTable = new DataTable();
                dataTable.Load(reader);
                AnalyticsGrid.ItemsSource = dataTable.DefaultView;

                using var cmdFunc = _context.Database.GetDbConnection().CreateCommand();
                // Підрахунок доходу з правильним JOIN
                cmdFunc.CommandText = @"
                    SELECT SUM(s.""Price"") 
                    FROM ""Ticket"" t 
                    JOIN ""Session"" s ON t.""SessionId"" = s.""SessionId"" 
                    WHERE t.""IsSold"" = true";

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
        // CRUD LOGIC (CINEMAS & HALLS)
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