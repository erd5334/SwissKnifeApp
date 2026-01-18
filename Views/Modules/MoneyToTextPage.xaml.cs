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
            var amountText = txtAmount?.Text?.Trim().Replace(',', '.') ?? "";
            if (!decimal.TryParse(amountText, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amount))
            {
                if (txtResult != null)
                    txtResult.Text = "Geçerli bir tutar girin.";
                return;
            }

            string? langText = null;
            if (cmbLanguage != null)
            {
                langText = cmbLanguage.SelectedValue as string;
                if (string.IsNullOrWhiteSpace(langText))
                {
                    if (cmbLanguage.SelectedItem is ComboBoxItem cbi)
                    {
                        langText = cbi.Tag as string ?? cbi.Content?.ToString();
                        if (string.IsNullOrWhiteSpace(langText) && cbi.Content is StackPanel sp)
                        {
                            var tb = sp.Children.OfType<TextBlock>().FirstOrDefault();
                            langText = tb?.Text;
                        }
                    }
                }
            }

            var language = _moneyToTextService.ParseLanguage(langText);
            var casing = _moneyToTextService.ParseCasingIndex(cmbCasing?.SelectedIndex);
            bool noSpaces = chkNoSpaces?.IsChecked == true;
            string separator = txtSeparator?.Text ?? " ";
            
            // Note: chkFirstLetterUpper was removed in modern UI as it is now part of cmbCasing or handled by service
            var result = _moneyToTextService.Convert(amount, language, casing, noSpaces, separator, false);
            if (txtResult != null)
                txtResult.Text = result;
        }

        private void btnCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResult?.Text))
                Clipboard.SetText(txtResult.Text);
        }
    }
}
