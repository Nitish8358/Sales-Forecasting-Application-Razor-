using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

public class SalesQueryModel : PageModel
{
    private readonly IConfiguration _configuration;

    public SalesQueryModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public int Year { get; set; }
    public decimal? TotalSales { get; set; }
    public decimal? ForecastedTotalSales { get; set; }
    public List<SalesByState> SalesByState { get; set; }
    public List<SalesByState> PaginatedSalesByState { get; set; }
    public decimal PercentageIncrease { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    private const int PageSize = 10;

    public void OnPost()
    {
        Year = int.Parse(Request.Form["year"]);
        PercentageIncrease = decimal.Parse(Request.Form["increase"]);
        CurrentPage = int.Parse(Request.Form["page"] ?? "1");

        string connectionString = "connection_string";
        using (SqlConnection connection = new SqlConnection(connectionString))
        {
            connection.Open();
            string query = @"
                Query 2";

            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Year", Year);
                command.Parameters.AddWithValue("@Offset", (CurrentPage - 1) * PageSize);
                command.Parameters.AddWithValue("@PageSize", PageSize);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        TotalSales = reader.GetDecimal(0);
                        ForecastedTotalSales = TotalSales * (1 + (PercentageIncrease / 100));
                    }
                    if (reader.NextResult())
                    {
                        SalesByState = new List<SalesByState>();
                        while (reader.Read())
                        {
                            var salesByState = new SalesByState
                            {
                                State = reader.GetString(0),
                                Sales = reader.GetDecimal(1)
                            };
                            salesByState.IncreasedSales = salesByState.Sales * (1 + (PercentageIncrease / 100));
                            SalesByState.Add(salesByState);
                        }
                        PaginatedSalesByState = SalesByState;
                    }
                    if (reader.NextResult() && reader.Read())
                    {
                        int totalRows = reader.GetInt32(0);
                        TotalPages = (int)Math.Ceiling(totalRows / (double)PageSize);
                    }
                }
            }
        }
    }

    public IActionResult OnPostDownloadCsv()
    {
        Year = int.Parse(Request.Form["year"]);
        PercentageIncrease = decimal.Parse(Request.Form["increase"]);

        OnPost();

        var csv = new StringBuilder();
        csv.AppendLine("State,Percentage Increase,Sales Value");

        foreach (var item in SalesByState)
        {
            csv.AppendLine($"{item.State},{PercentageIncrease.ToString(CultureInfo.InvariantCulture)},{item.IncreasedSales.ToString(CultureInfo.InvariantCulture)}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "forecasted_sales.csv");
    }
}

public class SalesByState
{
    public string State { get; set; }
    public decimal Sales { get; set; }
    public decimal IncreasedSales { get; set; }
}
