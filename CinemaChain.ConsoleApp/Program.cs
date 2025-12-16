using Microsoft.EntityFrameworkCore;
using Models = CinemaChain.Data.Models;

namespace CinemaChain.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {        
            Console.WriteLine("Welcome to CinemaChain Management!");

            char c = 's';

            while (c != '0')
            {
     
                switch (c)
                {
                    case '1':
                        CreateCinema();
                        break;
                    case '2':
                        ReadAllCinemas();
                        break;
                    case '3':
                        UpdateCinema();
                        break;
                    case '4':
                        DeleteCinema();
                        break;
                    case '5':
                        ReadCinemaWithHalls();
                        break;
                    case '6':
                        CreateHall();
                        break;
                    case '7':
                        UpdateHall();
                        break;
                    case '8':
                        DeleteHall();
                        break;
                    case '0':
                        Console.WriteLine("Goodbye!");
                        break;
                    default:
                        if (c != 's')
                        {
                            Console.WriteLine("Invalid choice. Please try again.");
                        }
                        break;
                }
                Console.WriteLine("\n Cinema Management");
                Console.WriteLine("1. Create a new Cinema");
                Console.WriteLine("2. Show all Cinemas");
                Console.WriteLine("3. Update a Cinema");
                Console.WriteLine("4. Delete a Cinema");
                Console.WriteLine("5. View Cinema with its Halls");

                Console.WriteLine("\n Hall Management ");
                Console.WriteLine("6. Add a new Hall to a Cinema");
                Console.WriteLine("7. Update a Hall");
                Console.WriteLine("8. Delete a Hall");

                Console.WriteLine("\n0. Exit");
                Console.Write("Your choice: ");

                string input = Console.ReadLine() ?? "";
                c = input.Length > 0 ? input[0] : ' '; 
            }
        }

  
        private static void CreateCinema()
        {
            try
            {
                Console.Write("Enter City: ");
                string city = Console.ReadLine() ?? "";
                Console.Write("Enter Address: ");
                string address = Console.ReadLine() ?? "";
                Console.Write("Enter Email: ");
                string email = Console.ReadLine() ?? "";

                var newCinema = new Models.Cinema { City = city, Address = address, Email = email };

                using (var db = new Models.CinemaDbContext())
                {
                    db.Cinemas.Add(newCinema);
                    db.SaveChanges();
                }
                Console.WriteLine($"Cinema successfully created!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        private static void ReadAllCinemas()
        {
            try
            {
                using (var db = new Models.CinemaDbContext())
                {
                    var cinemas = db.Cinemas.ToList();
                    if (!cinemas.Any())
                    {
                        Console.WriteLine("No cinemas found.");
                        return;
                    }

                    Console.WriteLine("List of Cinema:");
                    foreach (var cinema in cinemas)
                    {
                        Console.WriteLine($"ID: {cinema.CinemaId}, Address: {cinema.Address}, City: {cinema.City},  Email: {cinema.Email}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void UpdateCinema()
        {
            try
            {
                Console.Write("Enter Cinema ID to update: ");
                if (!int.TryParse(Console.ReadLine(), out int id))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {
                    var cinema = db.Cinemas.Find(id);
                    if (cinema == null)
                    {
                        Console.WriteLine("Cinema not found.");
                        return;
                    }

                    Console.Write($"New Address: ");
                    string newAddress = Console.ReadLine() ?? "";
                    if (!string.IsNullOrWhiteSpace(newAddress))
                    {
                        cinema.Address = newAddress;
                    }

                    Console.Write($"New City: ");
                    string newCity = Console.ReadLine() ?? "";
                    if (!string.IsNullOrWhiteSpace(newCity))
                    {
                        cinema.City = newCity;
                    }

                    Console.Write($"New Email: ");
                    string newEmail = Console.ReadLine() ?? "";
                    if (!string.IsNullOrWhiteSpace(newEmail))
                    {
                        cinema.Email = newEmail;
                    }

                    db.SaveChanges();
                    Console.WriteLine("Cinema updated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void DeleteCinema()
        {
            try
            {
                Console.Write("Enter Cinema ID to delete: ");
                if (!int.TryParse(Console.ReadLine(), out int id))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {
                    var cinema = db.Cinemas.Find(id);
                    if (cinema == null)
                    {
                        Console.WriteLine("Cinema not found.");
                        return;
                    }

                    db.Cinemas.Remove(cinema);
                    db.SaveChanges();
                    Console.WriteLine("Cinema deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void ReadCinemaWithHalls()
        {
            try
            {
                Console.Write("Enter Cinema ID to view: ");
                if (!int.TryParse(Console.ReadLine(), out int id))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {       
                    var cinema = db.Cinemas
                        .Include(c => c.Halls)
                        .FirstOrDefault(c => c.CinemaId == id);

                    if (cinema == null)
                    {
                        Console.WriteLine("Cinema not found.");
                        return;
                    }

                    Console.WriteLine($"Cinema: {cinema.Address}");
                    if (cinema.Halls != null && cinema.Halls.Any())
                    {
                        Console.WriteLine("Halls in this cinema:");
                        foreach (var hall in cinema.Halls)
                        {
                            Console.WriteLine($"Hall: {hall.Name} (ID: {hall.HallId}), Seats: {hall.SeatsCount}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("This cinema has no halls yet.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        private static void CreateHall()
        {
            try
            {
                Console.Write("Enter Cinema ID to add hall to: ");
                if (!int.TryParse(Console.ReadLine(), out int cinemaId))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {
                    var cinema = db.Cinemas.Find(cinemaId);
                    if (cinema == null)
                    {
                        Console.WriteLine("Cinema with this ID not found.");
                        return;
                    }

                    Console.Write("Enter new hall name: ");
                    string name = Console.ReadLine();

                    Console.Write("Enter number of seats: ");
                    int.TryParse(Console.ReadLine(), out int seats); 

                    var newHall = new Models.Hall
                    {
                        Name = name,
                        SeatsCount = seats,
                        CinemaId = cinemaId 
                    };

                    db.Halls.Add(newHall);
                    db.SaveChanges();
                    Console.WriteLine($"Hall '{name}' added to cinema '{cinema.Address}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        private static void UpdateHall()
        {
            try
            {
                Console.Write("Enter Hall ID to update: ");
                if (!int.TryParse(Console.ReadLine(), out int id))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {
                    var hall = db.Halls.Find(id);
                    if (hall == null)
                    {
                        Console.WriteLine("Hall not found.");
                        return;
                    }

                    Console.Write($"New Name: ");
                    string newName = Console.ReadLine() ?? "";
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        hall.Name = newName;
                    }

                    Console.Write($"New number of seats: ");
                    string seatsInput = Console.ReadLine() ?? "";
                    if (!string.IsNullOrWhiteSpace(seatsInput) && int.TryParse(seatsInput, out int newSeats))
                    {
                        hall.SeatsCount = newSeats;
                    }

                    db.SaveChanges();
                    Console.WriteLine("Hall updated.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        private static void DeleteHall()
        {
            try
            {
                Console.Write("Enter Hall ID to delete: ");
                if (!int.TryParse(Console.ReadLine(), out int id))
                {
                    Console.WriteLine("Invalid ID.");
                    return;
                }

                using (var db = new Models.CinemaDbContext())
                {
                    var hall = db.Halls.Find(id);
                    if (hall == null)
                    {
                        Console.WriteLine("Hall not found.");
                        return;
                    }

                    db.Halls.Remove(hall);
                    db.SaveChanges();
                    Console.WriteLine("Hall deleted.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}