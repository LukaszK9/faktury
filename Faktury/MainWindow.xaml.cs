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
        public ObservableCollection<Invoice> InvoicesList { get; set; }
        
        private Invoice _selectedInvoice;
        private bool IsEditMode => _selectedInvoice != null;

        public MainWindow()
        {
            InitializeComponent();

            CurrentInvoiceItems = new ObservableCollection<InvoiceItem>();
            ClientsList = new ObservableCollection<Client>();
            InvoicesList = new ObservableCollection<Invoice>();

            // TODO pobiera prawdziwe produkty
            ProductsList = new ObservableCollection<Product>()
            {
                new Product { Id = 1, Name = "Młotek", PriceNetto = 150.00m, Unit = "szt", Vat = 23 },
                new Product { Id = 2, Name = "Coś innego", PriceNetto = 11.00m, Unit = "godz", Vat = 10 }
            };

            Trace.WriteLine(ProductsList[0]);

            Invoice_DataGrid.ItemsSource = CurrentInvoiceItems;
            Clients_DataGrid.ItemsSource = ClientsList;
            Products_DataGrid.ItemsSource = ProductsList;
            Reports_DataGrid.ItemsSource = InvoicesList;

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

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Input_ProductName.Text))
            {
                MessageBox.Show("Nazwa produktu jest wymagana!", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int nextId = 1;
            if (ProductsList.Count > 0)
            {
                int maxId = 0;
                foreach (var p in ProductsList)
                {
                    if (p.Id > maxId) maxId = p.Id;
                }
                nextId = maxId + 1;
            }

            Product newProduct = new Product
            {
                Id = nextId,
                Name = Input_ProductName.Text,
                Unit = Input_ProductUnit.Text,
                PriceNetto = decimal.Parse(Input_ProductPrice.Text),
                Vat = decimal.Parse(Input_ProductVat.Text)
            };

            ProductsList.Add(newProduct);

            CleanProductForm_Click(null, null);
        }

        private void CleanProductForm_Click(object sender, RoutedEventArgs e)
        {
            Input_ProductName.Clear();
            Input_ProductUnit.Clear();
            Input_ProductPrice.Clear();
            Input_ProductVat.Clear();
        }

        private void SaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (Input_InvoiceClientName.SelectedItem == null)
            {
                MessageBox.Show("Wybierz klienta!");
                return;
            }

            if (CurrentInvoiceItems.Count == 0)
            {
                MessageBox.Show("Dodaj produkty!");
                return;
            }

            // EDIT MODE
            if (IsEditMode)
            {
                _selectedInvoice.Client = (Client)Input_InvoiceClientName.SelectedItem;
                _selectedInvoice.Items = new ObservableCollection<InvoiceItem>(CurrentInvoiceItems.ToList());

                Reports_DataGrid.Items.Refresh();

                MessageBox.Show("Faktura zaktualizowana!");

                return;
            }

            // NEW MODE
            int nextId = InvoicesList.Count > 0
                ? InvoicesList.Max(x => x.Id) + 1
                : 1;

            Invoice invoice = new Invoice
            {
                Id = nextId,
                Client = (Client)Input_InvoiceClientName.SelectedItem,
                Date = DateTime.Now,
                Items = new ObservableCollection<InvoiceItem>(CurrentInvoiceItems.ToList())
            };

            InvoicesList.Add(invoice);

            MessageBox.Show("Faktura zapisana!");
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            _selectedInvoice = null;

            CurrentInvoiceItems.Clear();
            Input_InvoiceClientName.SelectedIndex = -1;
        }

        private void Reports_DataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Reports_DataGrid.SelectedItem is not Invoice invoice)
                return;

            _selectedInvoice = invoice;

            // switch to Faktury tab
            Tab_Faktury.IsSelected = true;

            LoadInvoiceToForm(invoice);
        }

        private void LoadInvoiceToForm(Invoice invoice)
        {
            CurrentInvoiceItems.Clear();

            foreach (var item in invoice.Items)
                CurrentInvoiceItems.Add(item);

            Input_InvoiceClientName.SelectedItem = invoice.Client;
        }
    }

    public class InvoiceItem
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required double Count { get; set; }
        public required string Unit { get; set; }
        public required decimal PriceNetto { get; set; }
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

    public class Invoice
    {
        public int Id { get; set; }
        public Client Client { get; set; }
        public DateTime Date { get; set; }
        public ObservableCollection<InvoiceItem> Items { get; set; }
        public decimal TotalNetto => Items?.Sum(x => x.ValueNetto) ?? 0;
        public decimal TotalBrutto => Items?.Sum(x => x.ValueBrutto) ?? 0;
    }
}