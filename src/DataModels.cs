using System;
using System.Collections.Generic;
using System.Linq;

namespace DataScienceWorkbench
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class UserVisibleAttribute : Attribute
    {
        public string Description { get; private set; }

        public UserVisibleAttribute() { Description = null; }
        public UserVisibleAttribute(string description) { Description = description; }
    }

    public static class UserVisibleHelper
    {
        public static List<System.Reflection.PropertyInfo> GetVisibleProperties(Type type)
        {
            var allProps = type.GetProperties();
            var markedProps = new List<System.Reflection.PropertyInfo>();
            bool anyMarked = false;

            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;

                if (p.GetCustomAttributes(typeof(UserVisibleAttribute), true).Length > 0)
                {
                    markedProps.Add(p);
                    anyMarked = true;
                }
            }

            if (anyMarked)
                return markedProps;

            var result = new List<System.Reflection.PropertyInfo>();
            foreach (var p in allProps)
            {
                if (p.GetIndexParameters().Length > 0) continue;
                if (p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)) continue;
                if (p.PropertyType.IsClass && p.PropertyType != typeof(string)) continue;
                result.Add(p);
            }
            return result;
        }

        public static string GetPythonTypeName(Type t)
        {
            if (t == typeof(int) || t == typeof(long) || t == typeof(short) || t == typeof(byte)) return "int";
            if (t == typeof(double) || t == typeof(float) || t == typeof(decimal)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(DateTime)) return "datetime";
            return t.Name;
        }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Country { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class Customer
    {
        [UserVisible("Unique customer identifier")]
        public int Id { get; set; }

        [UserVisible("Customer's first name")]
        public string FirstName { get; set; }

        [UserVisible("Customer's last name")]
        public string LastName { get; set; }

        [UserVisible("Customer's email address")]
        public string Email { get; set; }

        [UserVisible("Customer's phone number")]
        public string Phone { get; set; }

        [UserVisible("Customer's date of birth")]
        public DateTime DateOfBirth { get; set; }

        [UserVisible("Date the customer registered")]
        public DateTime RegistrationDate { get; set; }

        [UserVisible("Loyalty tier: Bronze, Silver, Gold, or Platinum")]
        public string Tier { get; set; }

        [UserVisible("Maximum credit limit in dollars")]
        public double CreditLimit { get; set; }

        [UserVisible("Whether the customer account is currently active")]
        public bool IsActive { get; set; }

        public Address Address { get; set; }
        public List<Order> Orders { get; set; }

        [UserVisible("Computed full name (FirstName + LastName)")]
        public string FullName { get { return FirstName + " " + LastName; } }

        [UserVisible("Computed age in years based on DateOfBirth")]
        public int Age { get { return (int)((DateTime.Now - DateOfBirth).TotalDays / 365.25); } }
    }

    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string SKU { get; set; }
        public double Price { get; set; }
        public double Cost { get; set; }
        public int StockQuantity { get; set; }
        public int ReorderLevel { get; set; }
        public double Weight { get; set; }
        public string Supplier { get; set; }
        public double Rating { get; set; }
        public int ReviewCount { get; set; }
        public bool IsDiscontinued { get; set; }
        public DateTime DateAdded { get; set; }

        public double Margin { get { return Price - Cost; } }
        public double MarginPercent { get { return Cost > 0 ? (Margin / Cost) * 100 : 0; } }
    }

    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Discount { get; set; }

        public double LineTotal { get { return (UnitPrice * Quantity) * (1 - Discount); } }
    }

    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime? ShipDate { get; set; }
        public string Status { get; set; }
        public string ShipMethod { get; set; }
        public double ShippingCost { get; set; }
        public string PaymentMethod { get; set; }
        public List<OrderItem> Items { get; set; }

        public double Subtotal { get { return Items != null ? Items.Sum(i => i.LineTotal) : 0; } }
        public double Total { get { return Subtotal + ShippingCost; } }
        public int ItemCount { get { return Items != null ? Items.Sum(i => i.Quantity) : 0; } }
    }

    public class Employee
    {
        [UserVisible("Unique employee identifier")]
        public int Id { get; set; }

        [UserVisible("Employee's first name")]
        public string FirstName { get; set; }

        [UserVisible("Employee's last name")]
        public string LastName { get; set; }

        [UserVisible("Department name (e.g. Engineering, Sales, HR)")]
        public string Department { get; set; }

        [UserVisible("Job title within the department")]
        public string Title { get; set; }

        [UserVisible("Date the employee was hired")]
        public DateTime HireDate { get; set; }

        [UserVisible("Annual salary in dollars")]
        public double Salary { get; set; }

        [UserVisible("Performance review score from 0.0 to 5.0")]
        public double PerformanceScore { get; set; }

        [UserVisible("Id of the employee's direct manager (0 if none)")]
        public int ManagerId { get; set; }

        [UserVisible("Whether the employee works remotely")]
        public bool IsRemote { get; set; }

        [UserVisible("Office location name")]
        public string Office { get; set; }

        [UserVisible("Computed full name (FirstName + LastName)")]
        public string FullName { get { return FirstName + " " + LastName; } }

        [UserVisible("Computed years of employment based on HireDate")]
        public int YearsEmployed { get { return (int)((DateTime.Now - HireDate).TotalDays / 365.25); } }
    }

    public class SensorReading
    {
        public int SensorId { get; set; }
        public string SensorType { get; set; }
        public string Location { get; set; }
        public DateTime Timestamp { get; set; }
        public double Value { get; set; }
        public string Unit { get; set; }
        public string Status { get; set; }
        public double BatteryLevel { get; set; }
    }

    public class StockPrice
    {
        public string Symbol { get; set; }
        public string CompanyName { get; set; }
        public DateTime Date { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
        public double AdjClose { get; set; }
    }

    public class WebEvent
    {
        public string SessionId { get; set; }
        public string UserId { get; set; }
        public DateTime Timestamp { get; set; }
        public string EventType { get; set; }
        public string Page { get; set; }
        public string Referrer { get; set; }
        public string Browser { get; set; }
        public string Device { get; set; }
        public string Country { get; set; }
        public int Duration { get; set; }
    }

    public class DataGenerator
    {
        private Random rng;
        private static readonly string[] FirstNames = { "Alice", "Bob", "Charlie", "Diana", "Edward", "Fiona", "George", "Hannah", "Ivan", "Julia", "Kevin", "Laura", "Michael", "Nina", "Oscar", "Patricia", "Quinn", "Rachel", "Steven", "Tina", "Ulrich", "Victoria", "William", "Xena", "Yuri", "Zara" };
        private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White", "Harris", "Sanchez" };
        private static readonly string[] Cities = { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose", "Austin", "Jacksonville", "Fort Worth", "Columbus", "Charlotte", "Indianapolis", "San Francisco", "Seattle", "Denver", "Nashville" };
        private static readonly string[] States = { "NY", "CA", "IL", "TX", "AZ", "PA", "TX", "CA", "TX", "CA", "TX", "FL", "TX", "OH", "NC", "IN", "CA", "WA", "CO", "TN" };
        private static readonly string[] Countries = { "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA", "USA" };
        private static readonly string[] Categories = { "Electronics", "Clothing", "Home & Garden", "Sports", "Books", "Toys", "Food & Beverage", "Health", "Automotive", "Office Supplies" };
        private static readonly string[] SubCategories = { "Premium", "Standard", "Budget", "Eco-Friendly", "Limited Edition", "Clearance" };
        private static readonly string[] Suppliers = { "Acme Corp", "GlobalTech", "PrimeParts", "ValueSource", "QualityFirst", "BulkBuy", "DirectShip", "EcoSupply" };
        private static readonly string[] Departments = { "Engineering", "Sales", "Marketing", "Finance", "HR", "Operations", "Legal", "R&D", "Support", "Product" };
        private static readonly string[] Titles = { "Analyst", "Senior Analyst", "Manager", "Senior Manager", "Director", "VP", "Lead", "Specialist", "Coordinator", "Associate" };
        private static readonly string[] Offices = { "HQ - NYC", "West - SF", "South - Austin", "Midwest - Chicago", "Remote" };
        private static readonly string[] OrderStatuses = { "Pending", "Processing", "Shipped", "Delivered", "Cancelled", "Returned" };
        private static readonly string[] ShipMethods = { "Standard", "Express", "Overnight", "Economy", "Two-Day" };
        private static readonly string[] PaymentMethods = { "Credit Card", "Debit Card", "PayPal", "Wire Transfer", "Check" };
        private static readonly string[] Tiers = { "Bronze", "Silver", "Gold", "Platinum", "Diamond" };
        private static readonly string[] SensorTypes = { "Temperature", "Humidity", "Pressure", "Light", "Motion", "CO2", "Noise", "Vibration" };
        private static readonly string[] SensorLocations = { "Building A - Floor 1", "Building A - Floor 2", "Building B - Floor 1", "Building B - Floor 2", "Warehouse", "Parking Lot", "Roof", "Basement" };
        private static readonly string[] SensorUnits = { "°C", "%RH", "hPa", "lux", "count", "ppm", "dB", "mm/s" };
        private static readonly string[] Symbols = { "AAPL", "GOOGL", "MSFT", "AMZN", "TSLA", "META", "NVDA", "JPM", "V", "JNJ" };
        private static readonly string[] CompanyNames = { "Apple Inc.", "Alphabet Inc.", "Microsoft Corp.", "Amazon.com Inc.", "Tesla Inc.", "Meta Platforms", "NVIDIA Corp.", "JPMorgan Chase", "Visa Inc.", "Johnson & Johnson" };
        private static readonly string[] EventTypes = { "page_view", "click", "scroll", "form_submit", "purchase", "add_to_cart", "search", "video_play", "share", "signup" };
        private static readonly string[] Pages = { "/home", "/products", "/about", "/contact", "/blog", "/pricing", "/faq", "/dashboard", "/settings", "/checkout" };
        private static readonly string[] Referrers = { "google.com", "facebook.com", "twitter.com", "linkedin.com", "direct", "email", "reddit.com", "bing.com" };
        private static readonly string[] Browsers = { "Chrome", "Firefox", "Safari", "Edge", "Opera" };
        private static readonly string[] Devices = { "Desktop", "Mobile", "Tablet" };
        private static readonly string[] EventCountries = { "US", "UK", "CA", "DE", "FR", "AU", "JP", "BR", "IN", "MX" };

        public DataGenerator(int seed = 42)
        {
            rng = new Random(seed);
        }

        private string Pick(string[] arr) { return arr[rng.Next(arr.Length)]; }
        private double RandDouble(double min, double max) { return min + rng.NextDouble() * (max - min); }
        private DateTime RandDate(DateTime min, DateTime max)
        {
            int range = (max - min).Days;
            return min.AddDays(rng.Next(range));
        }

        public List<Product> GenerateProducts(int count = 200)
        {
            var products = new List<Product>();
            for (int i = 1; i <= count; i++)
            {
                string cat = Pick(Categories);
                products.Add(new Product
                {
                    Id = i,
                    Name = cat + " Item " + i,
                    Category = cat,
                    SubCategory = Pick(SubCategories),
                    SKU = "SKU-" + i.ToString("D6"),
                    Price = Math.Round(RandDouble(5, 500), 2),
                    Cost = Math.Round(RandDouble(2, 250), 2),
                    StockQuantity = rng.Next(0, 1000),
                    ReorderLevel = rng.Next(10, 100),
                    Weight = Math.Round(RandDouble(0.1, 50), 2),
                    Supplier = Pick(Suppliers),
                    Rating = Math.Round(RandDouble(1, 5), 1),
                    ReviewCount = rng.Next(0, 5000),
                    IsDiscontinued = rng.NextDouble() < 0.1,
                    DateAdded = RandDate(new DateTime(2020, 1, 1), new DateTime(2025, 12, 31))
                });
            }
            return products;
        }

        public List<Customer> GenerateCustomers(int count = 150, List<Product> products = null)
        {
            var customers = new List<Customer>();
            for (int i = 1; i <= count; i++)
            {
                int cityIdx = rng.Next(Cities.Length);
                var cust = new Customer
                {
                    Id = i,
                    FirstName = Pick(FirstNames),
                    LastName = Pick(LastNames),
                    Email = "user" + i + "@example.com",
                    Phone = "(" + rng.Next(200, 999) + ") " + rng.Next(200, 999) + "-" + rng.Next(1000, 9999),
                    DateOfBirth = RandDate(new DateTime(1960, 1, 1), new DateTime(2003, 1, 1)),
                    RegistrationDate = RandDate(new DateTime(2018, 1, 1), new DateTime(2025, 12, 31)),
                    Tier = Pick(Tiers),
                    CreditLimit = Math.Round(RandDouble(500, 50000), 2),
                    IsActive = rng.NextDouble() > 0.15,
                    Address = new Address
                    {
                        Street = rng.Next(100, 9999) + " " + Pick(LastNames) + " St",
                        City = Cities[cityIdx],
                        State = States[cityIdx],
                        ZipCode = rng.Next(10000, 99999).ToString(),
                        Country = Countries[cityIdx],
                        Latitude = Math.Round(RandDouble(25, 48), 6),
                        Longitude = Math.Round(RandDouble(-125, -70), 6)
                    },
                    Orders = new List<Order>()
                };
                customers.Add(cust);
            }
            return customers;
        }

        public List<Order> GenerateOrders(List<Customer> customers, List<Product> products, int count = 500)
        {
            var orders = new List<Order>();
            for (int i = 1; i <= count; i++)
            {
                var cust = customers[rng.Next(customers.Count)];
                var orderDate = RandDate(cust.RegistrationDate, new DateTime(2025, 12, 31));
                var status = Pick(OrderStatuses);
                DateTime? shipDate = null;
                if (status == "Shipped" || status == "Delivered")
                    shipDate = orderDate.AddDays(rng.Next(1, 14));

                int itemCount = rng.Next(1, 6);
                var items = new List<OrderItem>();
                for (int j = 0; j < itemCount; j++)
                {
                    var prod = products[rng.Next(products.Count)];
                    items.Add(new OrderItem
                    {
                        ProductId = prod.Id,
                        ProductName = prod.Name,
                        Quantity = rng.Next(1, 10),
                        UnitPrice = prod.Price,
                        Discount = rng.NextDouble() < 0.3 ? Math.Round(RandDouble(0.05, 0.25), 2) : 0
                    });
                }

                var order = new Order
                {
                    Id = i,
                    CustomerId = cust.Id,
                    OrderDate = orderDate,
                    ShipDate = shipDate,
                    Status = status,
                    ShipMethod = Pick(ShipMethods),
                    ShippingCost = Math.Round(RandDouble(0, 50), 2),
                    PaymentMethod = Pick(PaymentMethods),
                    Items = items
                };
                orders.Add(order);
                cust.Orders.Add(order);
            }
            return orders;
        }

        public List<Employee> GenerateEmployees(int count = 100)
        {
            var employees = new List<Employee>();
            for (int i = 1; i <= count; i++)
            {
                employees.Add(new Employee
                {
                    Id = i,
                    FirstName = Pick(FirstNames),
                    LastName = Pick(LastNames),
                    Department = Pick(Departments),
                    Title = Pick(Titles),
                    HireDate = RandDate(new DateTime(2010, 1, 1), new DateTime(2025, 6, 1)),
                    Salary = Math.Round(RandDouble(40000, 200000), 2),
                    PerformanceScore = Math.Round(RandDouble(1, 5), 2),
                    ManagerId = i > 10 ? rng.Next(1, 11) : 0,
                    IsRemote = rng.NextDouble() < 0.35,
                    Office = Pick(Offices)
                });
            }
            return employees;
        }

        public List<SensorReading> GenerateSensorReadings(int count = 1000)
        {
            var readings = new List<SensorReading>();
            for (int i = 0; i < count; i++)
            {
                int typeIdx = rng.Next(SensorTypes.Length);
                readings.Add(new SensorReading
                {
                    SensorId = rng.Next(1, 50),
                    SensorType = SensorTypes[typeIdx],
                    Location = Pick(SensorLocations),
                    Timestamp = RandDate(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)).AddSeconds(rng.Next(0, 86400)),
                    Value = Math.Round(RandDouble(-10, 100), 3),
                    Unit = SensorUnits[typeIdx],
                    Status = rng.NextDouble() < 0.95 ? "Normal" : "Alert",
                    BatteryLevel = Math.Round(RandDouble(10, 100), 1)
                });
            }
            return readings;
        }

        public List<StockPrice> GenerateStockPrices(int daysBack = 365)
        {
            var prices = new List<StockPrice>();
            var startDate = DateTime.Now.AddDays(-daysBack);
            for (int s = 0; s < Symbols.Length; s++)
            {
                double basePrice = RandDouble(50, 500);
                for (int d = 0; d < daysBack; d++)
                {
                    var date = startDate.AddDays(d);
                    if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

                    double change = RandDouble(-0.05, 0.05);
                    double open = Math.Round(basePrice, 2);
                    double close = Math.Round(basePrice * (1 + change), 2);
                    double high = Math.Round(Math.Max(open, close) * (1 + RandDouble(0, 0.02)), 2);
                    double low = Math.Round(Math.Min(open, close) * (1 - RandDouble(0, 0.02)), 2);
                    basePrice = close;

                    prices.Add(new StockPrice
                    {
                        Symbol = Symbols[s],
                        CompanyName = CompanyNames[s],
                        Date = date,
                        Open = open,
                        High = high,
                        Low = low,
                        Close = close,
                        Volume = (long)(rng.Next(1000000, 50000000)),
                        AdjClose = close
                    });
                }
            }
            return prices;
        }

        public List<WebEvent> GenerateWebEvents(int count = 2000)
        {
            var events = new List<WebEvent>();
            for (int i = 0; i < count; i++)
            {
                events.Add(new WebEvent
                {
                    SessionId = "sess_" + rng.Next(1, 500),
                    UserId = rng.NextDouble() < 0.7 ? "user_" + rng.Next(1, 200) : null,
                    Timestamp = RandDate(new DateTime(2025, 1, 1), new DateTime(2025, 12, 31)).AddSeconds(rng.Next(0, 86400)),
                    EventType = Pick(EventTypes),
                    Page = Pick(Pages),
                    Referrer = Pick(Referrers),
                    Browser = Pick(Browsers),
                    Device = Pick(Devices),
                    Country = Pick(EventCountries),
                    Duration = rng.Next(1, 600)
                });
            }
            return events;
        }
    }
}
