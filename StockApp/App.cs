using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;

public class App
{
    private const string USERNAME = "01143779@pw.edu.pl";
    private const string PASSWORD = "postman";

    private RestClient _client;
    private string[] _exchanges;

    public App()
    {
        _client = new RestClient("https://stockserver20201009223011.azurewebsites.net/");
        _client.Authenticator = new HttpBasicAuthenticator(USERNAME, PASSWORD);

        _exchanges = GetExchangeList();
    }





    public void MakeMonies()
    {
        Console.WriteLine("BEGIN MONIES-MAKING SESSION");
        float startFunds = GetClientData().funds;

        // buy 20 shares from each stock exchange except Warsaw
        BuyAllShares();

        Console.WriteLine("Waiting a minute before selling...");
        Thread.Sleep(60000);

        // sell all shares at Warsaw
        SellAllSharesAtWarsaw();

        // show client data
        float endFunds = GetClientData().funds;
        Console.WriteLine($"MADE {endFunds - startFunds} MONIES THIS SESSION");
    }




    public void BuyAllShares()
    {
        Console.WriteLine("BEGIN BUYING SHARES");

        int counter = 1;

        for (int i = 1; i < _exchanges.Length; i++)
        {
            string exchange = _exchanges[i];
            string[] stocks = GetSharesList(exchange);

            int left = 20;
            int j = 0;

            while (left > 0)
            {
                Console.Write($"{counter}| ");
                var stock = stocks[j];
                BuyShares(stock, exchange, 1);
                j++;
                if (j >= stocks.Length)
                    j = 0;
                left--;

                counter++;
            }
        }
    }
    public void SellAllSharesAtWarsaw()
    {
        Console.WriteLine("BEGIN SELLING SHARES");

        int counter = 1;

        Client client = GetClientData();
        foreach (var share in client.shares)
        {
            Console.Write($"{counter}| ");
            SellShares(share.Key, _exchanges[0], share.Value);
            counter++;
        }
    }







    public bool BuyShares(string stock, string exchange, int amount)
    {
        var price = GetSharePrice(stock, exchange);

        var request = new RestRequest("offer");
        var share = new Share()
        {
            stockExchange = exchange,
            share = stock,
            amount = amount,
            price = price.SellPrice,
            buySell = "buy"
        };

        request.AddJsonBody(share);

        IRestResponse response = _client.Post(request);

        if (response.Content.Length > 0)
        {
            Console.WriteLine($"Bought {amount} shares of {stock} at {exchange} for {share.price}");
            return true;
        }
        else
        {
            Console.WriteLine("BUYING UNSUCCESSFUL...");
            return false;
        }
    }
    public bool SellShares(string stock, string exchange, int amount)
    {
        var price = GetSharePrice(stock, exchange);

        var request = new RestRequest("offer");
        var share = new Share()
        {
            stockExchange = exchange,
            share = stock,
            amount = amount,
            price = price.BuyPrice,
            buySell = "sell"
        };

        request.AddJsonBody(share);

        IRestResponse response = _client.Post(request);

        if (response.Content.Length > 0)
        {
            Console.WriteLine($"Sold {amount} shares of {stock} at {exchange} for {share.price}");
            return true;
        }
        else
        {
            Console.WriteLine("SELLING UNSUCCESSFUL...");
            return false;
        }
    }





    private Client GetClientData()
    {
        var request = new RestRequest("client", DataFormat.Json);
        var responseJson = _client.Get(request);
        Client client = System.Text.Json.JsonSerializer.Deserialize<Client>(responseJson.Content);

        Console.WriteLine($"Funds: {client.funds}");

        return client;
    }
    private SharePrice GetSharePrice(string stock, string exchange)
    {
        var request = new RestRequest($"shareprice/{exchange}?share={stock}", DataFormat.Json);
        var responseJson = _client.Get(request);
        var content = responseJson.Content;

        var sellRegex = new Regex(".*\"price\":(.*),\"buySell\":\"sell\",\"amount\":(.*)}.*");
        float sellPrice = float.Parse(sellRegex.Match(content).Groups[1].Value, CultureInfo.InvariantCulture);
        int sellAmount = int.Parse(sellRegex.Match(content).Groups[2].Value);

        var buyRegex = new Regex(".*\"price\":(.*),\"buySell\":\"buy\",\"amount\":(.*)},{.*");
        float buyPrice = float.Parse(buyRegex.Match(content).Groups[1].Value, CultureInfo.InvariantCulture);
        int buyAmount = int.Parse(buyRegex.Match(content).Groups[2].Value);

        SharePrice price = new SharePrice()
        {
            SellPrice = sellPrice,
            SellAmount = sellAmount,
            BuyPrice = buyPrice,
            BuyAmount = buyAmount
        };

        return price;
    }
    private string[] GetExchangeList()
    {
        string[] responses = RequestList("stockexchanges");
        return responses;
    }
    private string[] GetSharesList(string exchange)
    {
        string[] responses = RequestList($"shareslist/{exchange}");
        return responses;
    }
    private string[] RequestList(string req)
    {
        var request = new RestRequest(req, DataFormat.Json);
        var responseJson = _client.Get(request);
        string[] responses = JsonConvert.DeserializeObject<string[]>(responseJson.Content);
        return responses;
    }
}