using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Printing.IndexedProperties;
using System.Text.Json;
using System.Windows;

namespace Faktury
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Client> ClientsList { get; set; }
        public ObservableCollection<Product> ProductsList { get; set; }
        public ObservableCollection<Invoice> InvoicesList { get; set; }
        
        private Invoice _selectedInvoice;
        private InvoiceItem _editingItem;
        private bool IsEditMode => _selectedInvoice != null;

        private const string InvoicesFile = "invoices.json";
        private const string ClientsFile = "clients.json";
        private const string ProductsFile = "products.json";

        public MainWindow()
        {
            InitializeComponent();

            InvoicesList = Load<Invoice>(InvoicesFile) ?? new ObservableCollection<Invoice>();
            ClientsList = Load<Client>(ClientsFile) ?? new ObservableCollection<Client>();
            ProductsList = Load<Product>(ProductsFile) ?? new ObservableCollection<Product>();

            foreach (var inv in InvoicesList)
            {
                inv.Items ??= new ObservableCollection<InvoiceItem>();
            }

            if (ProductsList.Count > 0)
                Trace.WriteLine(ProductsList[0]);

            Invoice_DataGrid.ItemsSource = null;
            Reports_DataGrid.ItemsSource = InvoicesList;
            Clients_DataGrid.ItemsSource = ClientsList;
            Products_DataGrid.ItemsSource = ProductsList;

            Input_InvoiceClientName.ItemsSource = ClientsList;
            Input_InvoiceClientName.DisplayMemberPath = "Name";

            Input_InvoiceItemName.ItemsSource = ProductsList;
            Input_InvoiceItemName.DisplayMemberPath = "Name";
        }

        private void Save<T>(string fileName, T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(fileName, json);
        }

        private ObservableCollection<T> Load<T>(string fileName)
        {
            if (!File.Exists(fileName))
                return new ObservableCollection<T>();

            var json = File.ReadAllText(fileName);

            var data = JsonSerializer.Deserialize<ObservableCollection<T>>(json)
                       ?? new ObservableCollection<T>();

            return data;
        }

        private void RecalculateItemIds()
        {
            if (_selectedInvoice?.Items == null)
                return;

            for (int i = 0; i < _selectedInvoice.Items.Count; i++)
            {
                _selectedInvoice.Items[i].Id = i + 1;
            }

            Invoice_DataGrid.Items.Refresh();
        }

        private void RecalculateInvoiceIds()
        {
            for (int i = 0; i < InvoicesList.Count; i++)
            {
                InvoicesList[i].Id = i + 1;
            }

            Reports_DataGrid.Items.Refresh();
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
            Save(ClientsFile, ClientsList);

            CleanClientForm_Click(null, null);

        }

        private void DeleteClient_Click(object sender, RoutedEventArgs e)
        {
            if (Clients_DataGrid.SelectedItem is not Client client)
                return;

            var result = MessageBox.Show(
                "Usunąć klienta?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            ClientsList.Remove(client);

            for (int i = 0; i < ClientsList.Count; i++)
            {
                ClientsList[i].Id = i + 1;
            }

            Clients_DataGrid.Items.Refresh();

            Save(ClientsFile, ClientsList);
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
            if (_selectedInvoice == null)
            {
                _selectedInvoice = new Invoice
                {
                    Id = InvoicesList.Count > 0 ? InvoicesList.Max(x => x.Id) + 1 : 1,
                    Client = null,
                    Date = DateTime.Now,
                    Items = new ObservableCollection<InvoiceItem>()
                };

                Invoice_DataGrid.ItemsSource = _selectedInvoice.Items;
            }

            if (Input_InvoiceItemName.SelectedItem is not Product selectedProduct)
                return;

            if (!double.TryParse(Input_InvoiceItemCount.Text, out double count))
                return;

            decimal vat = selectedProduct.Vat / 100m;
            decimal netto = selectedProduct.PriceNetto * (decimal)count;
            decimal brutto = netto + (netto * vat);

            var item = new InvoiceItem
            {
                Id = _selectedInvoice.Items.Count + 1,
                Name = selectedProduct.Name,
                Unit = selectedProduct.Unit,
                PriceNetto = selectedProduct.PriceNetto,
                Vat = selectedProduct.Vat,
                Count = count,
                PriceBrutto = Math.Round(selectedProduct.PriceNetto + selectedProduct.PriceNetto * vat, 2),
                ValueNetto = Math.Round(netto, 2),
                ValueBrutto = Math.Round(brutto, 2)
            };

            if (_editingItem != null)
            {
                int index = _selectedInvoice.Items.IndexOf(_editingItem);
                if (index >= 0)
                    _selectedInvoice.Items[index] = item;

                _editingItem = null;
                Invoice_DataGrid.Items.Refresh();
            }
            else
            {
                _selectedInvoice.Items.Add(item);
            }

            RecalculateItemIds();

            Input_InvoiceItemName.SelectedIndex = -1;
            Input_InvoiceItemCount.Clear();
        }

        private void DeleteInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (Reports_DataGrid.SelectedItem is not Invoice invoice)
                return;

            var result = MessageBox.Show(
                "Usunąć całą fakturę?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            InvoicesList.Remove(invoice);

            RecalculateInvoiceIds();

            Save(InvoicesFile, InvoicesList);

            Reports_DataGrid.Items.Refresh();
        }

        private void CleanInvoiceProductForm_Click(Object sender, RoutedEventArgs e)
        {
            Input_InvoiceItemName.SelectedIndex = -1; // None
            Input_InvoiceItemCount.Clear();
        }

        private void SaveInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (Input_InvoiceClientName.SelectedItem == null)
            {
                MessageBox.Show("Wybierz klienta!");
                return;
            }

            if (_selectedInvoice == null || _selectedInvoice.Items.Count == 0)
            {
                MessageBox.Show("Dodaj produkty!");
                return;
            }

            _selectedInvoice.Client = (Client)Input_InvoiceClientName.SelectedItem;
            _selectedInvoice.Date = Input_InvoiceDate.SelectedDate ?? DateTime.Now;

            if (!InvoicesList.Contains(_selectedInvoice))
            {
                InvoicesList.Add(_selectedInvoice);
            }

            Save(InvoicesFile, InvoicesList);

            Reports_DataGrid.Items.Refresh();

            MessageBox.Show("Faktura zapisana!");

            _selectedInvoice = null;
            Invoice_DataGrid.ItemsSource = null;
        }

        private void EditInvoiceItem_Click(object sender, RoutedEventArgs e)
        {
            if (Invoice_DataGrid.SelectedItem is not InvoiceItem item)
                return;

            _editingItem = item;

            Input_InvoiceItemName.SelectedItem =
                ProductsList.FirstOrDefault(p => p.Name == item.Name);

            Input_InvoiceItemCount.Text = item.Count.ToString();

            MessageBox.Show("Edytuj dane i kliknij 'Dodaj pozycję' (zastąpi starą)");
        }

        private void DeleteInvoiceItem_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedInvoice == null)
                return;

            if (Invoice_DataGrid.SelectedItem is not InvoiceItem item)
                return;

            var result = MessageBox.Show(
                "Usunąć tę pozycję?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            _selectedInvoice.Items.Remove(item);

            RecalculateItemIds();

            Save(InvoicesFile, InvoicesList);

            if (_selectedInvoice.Items.Count == 0)
            {
                InvoicesList.Remove(_selectedInvoice);
                _selectedInvoice = null;

                Invoice_DataGrid.ItemsSource = null;

                RecalculateInvoiceIds();
            }

            Save(InvoicesFile, InvoicesList);
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            _selectedInvoice = new Invoice
            {
                Id = InvoicesList.Count > 0 ? InvoicesList.Max(x => x.Id) + 1 : 1,
                Client = null,
                Date = DateTime.Now,
                Items = new ObservableCollection<InvoiceItem>()
            };

            Invoice_DataGrid.ItemsSource = _selectedInvoice.Items;

            Input_InvoiceClientName.SelectedIndex = -1;
            Invoice_ModeElement.Visibility = Visibility.Collapsed;
        }

        private void LoadInvoiceToForm(Invoice invoice)
        {
            _selectedInvoice = invoice;

            Invoice_DataGrid.ItemsSource = null;
            Invoice_DataGrid.ItemsSource = _selectedInvoice.Items;

            Input_InvoiceClientName.SelectedItem = invoice.Client;
            Input_InvoiceDate.SelectedDate = invoice.Date;

            Invoice_ModeElement.Visibility = Visibility.Visible;
            Invoice_ModeText.Text = $"Edytujesz Fakturę nr. {invoice.Id}";
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
            Save(ProductsFile, ProductsList);

            CleanProductForm_Click(null, null);
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            if (Products_DataGrid.SelectedItem is not Product product)
                return;

            var result = MessageBox.Show(
                "Usunąć produkt?",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            ProductsList.Remove(product);

            for (int i = 0; i < ProductsList.Count; i++)
            {
                ProductsList[i].Id = i + 1;
            }

            Products_DataGrid.Items.Refresh();

            Save(ProductsFile, ProductsList);
        }

        private void CleanProductForm_Click(object sender, RoutedEventArgs e)
        {
            Input_ProductName.Clear();
            Input_ProductUnit.Clear();
            Input_ProductPrice.Clear();
            Input_ProductVat.Clear();
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

    }

    public class InvoiceItem : INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        public required string Name { get; set; }
        public required double Count { get; set; }
        public required string Unit { get; set; }
        public required decimal PriceNetto { get; set; }
        public required decimal PriceBrutto { get; set; }
        public required decimal Vat { get; set; }
        public required decimal ValueNetto { get; set; }
        public required decimal ValueBrutto { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
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
        public ObservableCollection<InvoiceItem> Items { get; set; } = new();

        public decimal TotalNetto => Items.Sum(x => x.ValueNetto);
        public decimal TotalBrutto => Items.Sum(x => x.ValueBrutto);
    }
}