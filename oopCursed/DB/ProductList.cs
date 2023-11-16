﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;


namespace oopCursed.DB
{
    public class ProductList
    {

        public ObservableCollection<Product> Products { get; set; }  // Collection to store products
        private ProductManagerContext dbContext; // Database context

        // Constructor
        public ProductList()
        {
            Products = new ObservableCollection<Product>(); // Initialize the product collection
            dbContext = new ProductManagerContext(); // Initialize the database context
            LoadProductsFromDatabase();
        }

        // Add a new product to the collection and database
        public void AddProduct(Product newProduct)
        {
            try
            {
                newProduct.Id = 0; // Assign Id of 0 to the newly added product
                newProduct.WarehouseId = UserSession.WarehouseId;
                Products.Add(newProduct);
                dbContext.Products.Add(newProduct); // Add the new product to the database
                dbContext.SaveChanges(); // Save changes to the database
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, show an error message)
                Console.WriteLine($"Error adding product: {ex.Message}");
            }
        }
        public void RemoveProduct(Product productToRemove)
        {
            try
            {
                Products.Remove(productToRemove); // Remove from the collection
                dbContext.Products.Remove(productToRemove); // Remove from the database
                dbContext.SaveChanges(); // Save changes to the database
            }
            catch (Exception ex)
            {
                // Handle the exception (e.g., log it, show an error message)
                Console.WriteLine($"Error removing product: {ex.Message}");
            }
        }

        public void ReadProductsFromFile(string filePath)
        {
            try
            {
                // Read all lines from the file
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    // Split the line into individual values
                    string[] values = line.Split(',');

                    // Check if the line contains the expected number of values
                    if (values.Length == 7)
                    {
                        // Parse values and create a new Product
                        string productName = values[0].Trim();
                        int productPrice = int.Parse(values[1].Trim());
                        DateTime manufactureDate = DateTime.Parse(values[2].Trim());
                        string productType = values[3].Trim();
                        int productQuantity = int.Parse(values[4].Trim());
                        DateTime shelfLife = DateTime.Parse(values[5].Trim());
                        string character = values[6].Trim();

                        // Create a new Product
                        Product newProduct = new Product
                        {
                            Name = productName,
                            Price = productPrice,
                            ManufactureDate = manufactureDate,
                            Type = productType,
                            Quantity = productQuantity,
                            ShelfLife = shelfLife,
                            Character = character
                        };

                        // Add the new product to the collection
                        AddProduct(newProduct);
                    }
                    else
                    {
                        // Log a warning or handle invalid lines as needed
                        Console.WriteLine($"Invalid line: {line}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log, display an error message)
                Console.WriteLine($"Error reading products from file: {ex.Message}");
            }
        }



        // Load products from the database based on the current user's ID
        public void LoadProductsFromDatabase()
        {
            if (UserSession.UserId != 0)
            {
                // Fetch products from the database for the current user and add them to the ObservableCollection
                var productsFromDatabase = dbContext.Products
                                                    .Where(p => p.Warehouse.Userid == UserSession.UserId)
                                                    .ToList();

                Products.Clear(); // Clear existing products
                foreach (var product in productsFromDatabase)
                {
                    Products.Add(product);
                }
            }
        }
        // Get products expiring in a specific month
        public ObservableCollection<Product> GetProductsExpiringInMonth(int selectedMonth)
        {
            // Return the filtered collection based on the selected month
            return new ObservableCollection<Product>(Products
                .Where(p => p.ShelfLife.HasValue && p.ShelfLife.Value.Month == selectedMonth));
        }

        // Get products within a specified price range
        public ObservableCollection<Product> GetProductsInPriceRange(float minPrice, float maxPrice)
        {
            // Return the filtered collection based on the price range
            return new ObservableCollection<Product>(Products
                .Where(p => p.Price >= minPrice && p.Price <= maxPrice));
        }

        public string GetTypeWithShortestAverageStorageTerm()
        {
            var typeWithShortestAverageStorageTerm = Products
                .GroupBy(p => p.Type)
                .Select(group => new
                {
                    Type = group.Key,
                    AverageStorageTerm = group.Average(p => ((p.ShelfLife - p.ManufactureDate)?.Days) ?? 0)
                })
                .OrderBy(item => item.AverageStorageTerm)
                .FirstOrDefault()?.Type;

            return typeWithShortestAverageStorageTerm;
        }


        public List<KeyValuePair<string, decimal?>> GetTotalCostByTypeSorted()
        {
            var totalCostByType = Products
                .GroupBy(p => p.Type)
                .ToDictionary(g => g.Key, g => (decimal?)g.Sum(p => p.Price * p.Quantity));

            var sortedTotalCostByType = QuickSort(totalCostByType.ToList());

            return sortedTotalCostByType;
        }

        private List<KeyValuePair<string, decimal?>> QuickSort(List<KeyValuePair<string, decimal?>> list)
        {
            if (list.Count <= 1)
                return list;

            int pivotIndex = list.Count / 2;
            var pivot = list[pivotIndex];
            list.RemoveAt(pivotIndex);

            var lesser = new List<KeyValuePair<string, decimal?>>();
            var greater = new List<KeyValuePair<string, decimal?>>();

            foreach (var element in list)
            {
                if (element.Value <= pivot.Value)
                    lesser.Add(element);
                else
                    greater.Add(element);
            }

            var sortedList = new List<KeyValuePair<string, decimal?>>();
            sortedList.AddRange(QuickSort(lesser));
            sortedList.Add(pivot);
            sortedList.AddRange(QuickSort(greater));

            return sortedList;
        }

        public Dictionary<string, List<Product>> GetProductsByManufactureDateAndType()
        {
            var productsByManufactureDateAndType = Products
                .Where(p => p.ManufactureDate.HasValue) // Фільтрація за наявністю дати виготовлення
                .GroupBy(p => new { p.ManufactureDate.Value.Date, p.Type }) // Групування за датою виготовлення та типом
                .ToDictionary(
                    g => $"{g.Key.Type}-{g.Key.Date:yyyy-MM-dd}",
                    g => g.ToList()
                );

            return productsByManufactureDateAndType;
        }

        public Dictionary<int?, List<Product>> GetProductsByPrice()
        {
            var productsByPrice = Products
                .GroupBy(p => p.Price) // Групуємо товари за ціною
                .ToDictionary(
                    g => g.Key, // Ключ - ціна товару
                    g => g.ToList()
                );

            return productsByPrice;
        }


    }
}
