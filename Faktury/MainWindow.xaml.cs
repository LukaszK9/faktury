using System.Collections.ObjectModel;
using System.Windows;

namespace Faktury
{
    public partial class MainWindow : Window
    {
        // after adding/removing item to ObservableCollection UI should update automatically
        public ObservableCollection<InvoiceItem> CurrentInvoiceItems { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            CurrentInvoiceItems = new ObservableCollection<InvoiceItem>();
            Invoice_DataGrid.ItemsSource = CurrentInvoiceItems;
        }
    }

    public class InvoiceItem
    {
        public int Id { get; set; }
        public string Nazwa { get; set; }
        public double Ilosc { get; set; }
        public string JednostkaMiary { get; set; }
        public decimal CenaNetto { get; set; } // TODO: Stawka podatku / obliczanie automatycznie ceny netto na podstawie brutto i na odwrot
        public decimal CenaBrutto { get; set; }
        public decimal Vat { get; set; }

        // TODO: Liczenie wartosci na podrawie ilosc * cena
        public decimal WartoscNetto { get; set; }
        public decimal WartoscBrutto { get; set; }
    }

}