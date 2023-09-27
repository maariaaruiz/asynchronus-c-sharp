﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using JetBrains.Annotations;
using StockAnalyzer.Core;

namespace StockAnalyzer.CrossPlatform;

public partial class MainWindow : Window
{
    public DataGrid Stocks => this.FindControl<DataGrid>(nameof(Stocks));
    public ProgressBar StockProgress => this.FindControl<ProgressBar>(nameof(StockProgress));
    public TextBox StockIdentifier => this.FindControl<TextBox>(nameof(StockIdentifier));
    public Button Search => this.FindControl<Button>(nameof(Search));
    public TextBox Notes => this.FindControl<TextBox>(nameof(Notes));
    public TextBlock StocksStatus => this.FindControl<TextBlock>(nameof(StocksStatus));
    public TextBlock DataProvidedBy => this.FindControl<TextBlock>(nameof(DataProvidedBy));
    public TextBlock IEX => this.FindControl<TextBlock>(nameof(IEX));
    public TextBlock IEX_Terms => this.FindControl<TextBlock>(nameof(IEX_Terms));

    public MainWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        IEX.PointerPressed += (e, a) => Open("https://iextrading.com/developer/");
        IEX_Terms.PointerPressed += (e, a) => Open("https://iextrading.com/api-exhibit-a/");

        /// Data provided for free by <a href="https://iextrading.com/developer/" RequestNavigate="Hyperlink_OnRequestNavigate">IEX</Hyperlink>. View <Hyperlink NavigateUri="https://iextrading.com/api-exhibit-a/" RequestNavigate="Hyperlink_OnRequestNavigate">IEX’s Terms of Use.</Hyperlink>
    }


    private static string API_URL = "https://ps-async.fekberg.com/api/stocks";
    private Stopwatch stopwatch = new Stopwatch();

    [CanBeNull] CancellationTokenSource cancellationTokenSource;
    private void Search_Click(object sender, RoutedEventArgs e)
    {
        if (cancellationTokenSource is not null)
        {
            //already have an instance of the cancellation token source?
            //this means the button has already pressed!
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            Search.Content = "Search";
            return;
        }
        
        try
        {
            cancellationTokenSource = new();
            cancellationTokenSource.Token.Register(() =>
            {
                Notes.Text = "Cancellation request";
            });
            Search.Content = "Cancel";
         
            BeforeLoadingStockData();
            
            var loadLinesTask = SearchForStocks(cancellationTokenSource.Token);
            
            loadLinesTask.ContinueWith(
                t =>
                {
                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Notes.Text = t.Exception?.InnerException?.Message;
                    });
                },
                TaskContinuationOptions.OnlyOnFaulted);

            var processStockTask =
                loadLinesTask
                    .ContinueWith((completedTask) =>
                    {
                        var lines = completedTask.Result;
                        var data = new List<StockPrice>();
                        foreach (var line in lines.Skip(1))
                        {
                            var price = StockPrice.FromCSV(line);
                            data.Add(price);
                        }

                        Dispatcher.UIThread.InvokeAsync(() =>
                            Stocks.Items = data.Where(sp => sp.Identifier == StockIdentifier.Text));
                    },
                        cancellationTokenSource.Token,
                    TaskContinuationOptions.OnlyOnRanToCompletion,
                        TaskScheduler.Current);

            processStockTask.ContinueWith(_ =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                    AfterLoadingStockData());
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;

                Search.Content = "Search";
            });
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }

    private static Task<List<string>> SearchForStocks(
        CancellationToken cancellationToken)
    {
        return Task.Run(async () =>
        {
            using var stream = new StreamReader(File.OpenRead("StockPrices_Small.csv"));
            var lines = new List<string>();

            while (await stream.ReadLineAsync() is string line)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                lines.Add(line);
            }

            return lines;
        }, cancellationToken);
    }

    private async Task GetStock()
    {
        try
        {
            var store = new DataStore();

            var responseTask = store.GetStockPrices(StockIdentifier.Text);

            Stocks.Items = await responseTask;
        }
        catch (Exception ex)
        {
            Notes.Text = ex.Message;
        }
    }

    private void BeforeLoadingStockData()
    {
        stopwatch.Restart();
        StockProgress.IsVisible = true;
        StockProgress.IsIndeterminate = true;
    }

    private void AfterLoadingStockData()
    {
        StocksStatus.Text = $"Loaded stocks for {StockIdentifier.Text} in {stopwatch.ElapsedMilliseconds}ms";
        StockProgress.IsVisible = false;
    }

    private void Close_OnClick(object sender, RoutedEventArgs e)
    {
        if (Application.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            desktopLifetime.Shutdown();
        }
    }

    public static void Open(string url)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }
    }
}