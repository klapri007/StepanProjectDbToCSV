using System.Data.SqlClient;
using System.Globalization;
using System.Net;

namespace KurzovniListekStepan
{
    internal class VstupOdUzivatele
    {
        private object propojeni;

        public void Run()
        {
            WebClient client = new WebClient();
            Console.WriteLine("Ahoj, k jakemu datu chcete kuzovni listek? pokud nic nezadate nebo zadate nesmysl/datum v budoucnosti, vezme to dnesni kurz");
            var userDate = Console.ReadLine();
            CultureInfo culture = new CultureInfo("cs-CZ");
            string formattedDate = userDate.ToString();
            string url = $"https://www.cnb.cz/cs/financni-trhy/devizovy-trh/kurzy-devizoveho-trhu/kurzy-devizoveho-trhu/denni_kurz.txt?date={formattedDate}";
            client.DownloadFile(url, "dailyExchangeRates.cvs");
            using var sr = new StreamReader("./dailyExchangeRates.cvs");
            var line = string.Empty;
            string exchangeRate = null;
            string firstLine = string.Empty;

            using (var promena = new StreamReader("dailyExchangeRates.cvs"))
            {
                firstLine = sr.ReadLine();
            }

            // Odstraňte část s číslem dne v roce
            if (firstLine.Contains("#"))
            {
                firstLine = firstLine.Split('#')[0].Trim();
            }

            // Rozdělte datum pomocí tečky
            string[] rozdelenyDatum = firstLine.Split('.');

            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("USA"))
                {
                    var cols = line.Split('|');
                    exchangeRate = cols[4];
                    Console.WriteLine(exchangeRate);
                }
            }
            try
            {
                SqlConnectionStringBuilder propojeniDb = new SqlConnectionStringBuilder();
                propojeniDb.DataSource = "stbechyn-sql.database.windows.net";
                propojeniDb.UserID = "prvniit";
                propojeniDb.Password = "P@ssW0rd!";
                propojeniDb.InitialCatalog = "AdventureWorksDW2020";

                using (SqlConnection spojeni = new SqlConnection(propojeniDb.ConnectionString))
                {
                    String sql = "SELECT EnglishProductName, DealerPrice, DealerPrice * @exchangeRate FROM DimProduct WHERE DealerPrice IS NOT NULL";

                    using (SqlCommand command = new SqlCommand(sql, spojeni))
                    {
                        command.Parameters.AddWithValue("@exchangeRate", exchangeRate);

                        spojeni.Open();
                        

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            using var writer = new StreamWriter($"./{rozdelenyDatum[2]}_{rozdelenyDatum[1]}_{rozdelenyDatum[0]}-klapriPriceList.csv", false);
                            writer.WriteLine("Date;English ProductName;Dealer PriceUSD;Dealer PriceCZK");

                            while (reader.Read())
                            {
                                writer.WriteLine(
                                    $"{rozdelenyDatum[2]}_{rozdelenyDatum[1]}_{rozdelenyDatum[0]}" +
                                    $"{reader.GetString(0)};" +
                                    $"{reader.GetSqlMoney(1).ToDecimal().ToString(new CultureInfo("cs-CZ"))};" +
                                    $"{reader.GetSqlDouble(2).ToString().ToString(new CultureInfo("cs-CZ"))}"
                                    );
                            }
                        }
                    }
                }
                Console.WriteLine("Ukol byl splnen :)");
            }
            catch (SqlException exce)
            {
                Console.WriteLine(exce.ToString());
            }
        }
    }
}