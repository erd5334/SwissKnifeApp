using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HBMoneyToWords;
using HBMoneyToWords.Extensions;
using HBMoneyToWords.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SwissKnifeApp.Views.Modules
{
    /// <summary>
    /// Interaction logic for MoneyToTextPage.xaml
    /// </summary>
    public partial class MoneyToTextPage : Page
    {
        private readonly SwissKnifeApp.Services.MoneyToTextService _moneyToTextService;
        public MoneyToTextPage() : this(new SwissKnifeApp.Services.MoneyToTextService()) { }

        public MoneyToTextPage(SwissKnifeApp.Services.MoneyToTextService service)
        {
            _moneyToTextService = service;
            InitializeComponent();
        }

        private void BtnConvert_Click(object sender, RoutedEventArgs e)
        {
            var txtAmountBox = FindName("txtAmount") as TextBox;
            var txtResultBlock = FindName("txtResult") as TextBlock;
            var cmbLanguage = FindName("cmbLanguage") as ComboBox;
            var cmbCasing = FindName("cmbCasing") as ComboBox;
            var chkNoSpaces = FindName("chkNoSpaces") as CheckBox;
            var chkFirstLetterUpper = FindName("chkFirstLetterUpper") as CheckBox;
            var txtSeparator = FindName("txtSeparator") as TextBox;
            var amountText = txtAmountBox?.Text?.Trim().Replace(',', '.') ?? "";
            if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                if (txtResultBlock != null)
                    txtResultBlock.Text = "Geçerli bir tutar girin.";
                return;
            }

            var langText = (cmbLanguage?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var language = _moneyToTextService.ParseLanguage(langText);
            var casing = _moneyToTextService.ParseCasingIndex(cmbCasing?.SelectedIndex);
            bool noSpaces = chkNoSpaces?.IsChecked == true;
            string separator = txtSeparator?.Text ?? " ";
            bool firstLetterUpper = chkFirstLetterUpper?.IsChecked == true;

            var result = _moneyToTextService.Convert(amount, language, casing, noSpaces, separator, firstLetterUpper);
            if (txtResultBlock != null)
                txtResultBlock.Text = result;
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText((FindName("txtResult") as TextBlock)?.Text ?? "");
        }
    }
}
