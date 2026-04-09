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

        public MainWindow()
        {
            InitializeComponent();

            CurrentInvoiceItems = new ObservableCollection<InvoiceItem>();
            ClientsList = new ObservableCollection<Client>();

            Invoice_DataGrid.ItemsSource = CurrentInvoiceItems;
            Clients_DataGrid.ItemsSource = ClientsList;
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

        // TODO: Liczenie wartosci na podrawie ilosc * cena
        public required decimal ValueNetto { get; set; }
        public required decimal ValueBrutto { get; set; }
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