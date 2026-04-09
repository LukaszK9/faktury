using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Printing.IndexedProperties;
using System.Windows;

namespace Faktury
{
    public partial class MainWindow : Window
    {
        // after adding/removing item to ObservableCollection UI should update automatically
        public ObservableCollection<InvoiceItem> CurrentInvoiceItems { get; set; }
        public ObservableCollection<Client> ClientsList { get; set; }
        public ObservableCollection<Product> ProductsList { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            CurrentInvoiceItems = new ObservableCollection<InvoiceItem>();
            ClientsList = new ObservableCollection<Client>();

            // TODO pobiera prawdziwe produkty
            ProductsList = new ObservableCollection<Product>()
            {
                new Product { Id = 1, Name = "Młotek", PriceNetto = 150.00m, Unit = "szt", Vat = 23 },
                new Product { Id = 2, Name = "Coś innego", PriceNetto = 11.00m, Unit = "godz", Vat = 10 }
            };

            Trace.WriteLine(ProductsList[0]);

            Invoice_DataGrid.ItemsSource = CurrentInvoiceItems;
            Clients_DataGrid.ItemsSource = ClientsList;

            Input_InvoiceClientName.ItemsSource = ClientsList;
            Input_InvoiceClientName.DisplayMemberPath = "Name";

            Input_InvoiceItemName.ItemsSource = ProductsList;
            Input_InvoiceItemName.DisplayMemberPath = "Name";
        }

        private void AddClient_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Input_ClientName.Text))
            {
                MessageBox.Show("Nazwa klienta jest wymagana!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            };

            int nextId = 1;
            if (ClientsList.Count > 0)
            {
                int maxId = 0;
                foreach (var k in ClientsList)
                {
                    if (k.Id > maxId) maxId = k.Id;
                }
                nextId = maxId + 1;
            }

            Client newClient = new Client
            {
                Id = nextId,
                Name = Input_ClientName.Text,
                Nip = Input_ClientNIP.Text,
                Adress = Input_ClientAdress.Text,
                Contact = Input_ClientContact.Text
            };

            ClientsList.Add(newClient);

            CleanClientForm_Click(null, null);

            MessageBox.Show("Klient został dodany pomyślnie!", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CleanClientForm_Click(object sender, RoutedEventArgs e)
        {
            Input_ClientName.Clear();
            Input_ClientNIP.Clear();
            Input_ClientAdress.Clear();
            Input_ClientContact.Clear();
        }

        private void AddInvoiceProduct_Click(object sender, RoutedEventArgs e)
        {
            Product selectedProduct = Input_InvoiceItemName.SelectedItem as Product;

            if (selectedProduct == null)
            {
                MessageBox.Show("Wybierz produkt z listy!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            };

            if (!double.TryParse(Input_InvoiceItemCount.Text, out double count))
            {
                MessageBox.Show("Ilość musi być poprawną liczbą!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            };

            decimal vatMultiplayer = selectedProduct.Vat / 100m;
            decimal priceBrutto = selectedProduct.PriceNetto + (selectedProduct.PriceNetto * vatMultiplayer);
            decimal valueNetto = selectedProduct.PriceNetto * (decimal)count;
            decimal valueBrutto = valueNetto + (valueNetto * vatMultiplayer);

            int nextId = 1;
            if (CurrentInvoiceItems.Count > 0)
            {
                int maxId = 0;
                foreach (var k in CurrentInvoiceItems)
                {
                    if (k.Id > maxId) maxId = k.Id;
                }
                nextId = maxId + 1;
            }

            InvoiceItem newItem = new InvoiceItem
            {
                Id = nextId,
                Name = selectedProduct.Name,
                Unit = selectedProduct.Unit,
                PriceNetto = selectedProduct.PriceNetto,
                Vat = selectedProduct.Vat,
                Count = count,
                PriceBrutto = Math.Round(priceBrutto, 2),
                ValueNetto = Math.Round(valueNetto, 2),
                ValueBrutto = Math.Round(valueBrutto, 2)
            };

            CurrentInvoiceItems.Add(newItem);

            CleanInvoiceProductForm_Click(null, null);
        }

        private void CleanInvoiceProductForm_Click(Object sender, RoutedEventArgs e)
        {
            Input_InvoiceItemName.SelectedIndex = -1; // None
            Input_InvoiceItemCount.Clear();
        }
    }

    public class InvoiceItem
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required double Count { get; set; }
        public required string Unit { get; set; }
        public required decimal PriceNetto { get; set; } // TODO: Stawka podatku / obliczanie automatycznie ceny netto na podstawie brutto i na odwrot
        public required decimal PriceBrutto { get; set; }
        public required decimal Vat { get; set; }

        public required decimal ValueNetto { get; set; }
        public required decimal ValueBrutto { get; set; }
    }

    public class Product
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Unit { get; set; }
        public required decimal PriceNetto { get; set; }
        public required decimal Vat { get; set; }
    }


    public class Client
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Nip { get; set; }
        public required string Adress { get; set; }
        public required string Contact { get; set; }
    }


}