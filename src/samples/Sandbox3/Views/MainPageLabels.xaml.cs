﻿using AppoMobi.Maui.Gestures;

namespace Sandbox.Views
{



    public partial class MainPageLabels : ContentPage
    {




        public MainPageLabels()
        {
            try
            {
                InitializeComponent();

                //avoid setting context BEFORE InitializeComponent, can bug 
                //having parent BindingContext still null when constructing from xaml
                BindingContext = new MainPageViewModel();

                //RiveoTest();

            }
            catch (Exception e)
            {
                Super.DisplayException(this, e);
                Console.WriteLine(e);
            }
        }







        private void HandleLinkTapped(object sender, string e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await App.Current.MainPage.DisplayAlert("Link Tapped", e, "OK");
            });
        }

        private void OnSpanTapped(object sender, TouchActionEventArgs e)
        {
            if (sender is TextSpan span)
            {
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await App.Current.MainPage.DisplayAlert("Span Tapped", span.Text, "OK");
                });
            }
        }
    }
}