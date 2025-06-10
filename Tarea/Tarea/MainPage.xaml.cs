namespace Tarea
{
    public partial class MainPage : ContentPage
    {
        int activaciones = 0;

        public MainPage()
        {
            InitializeComponent();
        }

        private void Activar_Clicked(object sender, EventArgs e)
        {
            activaciones++;
            CounterLabel.Text = $"Botón presionado {activaciones} veces.";

            List<string> preferencias = new List<string>();

            // Validar colores
            if (chk_colorazul.IsChecked)
                preferencias.Add("Color: Azul");
            if (chk_colorrojo.IsChecked)
                preferencias.Add("Color: Rojo");

            // Validar galleta
            if (Rb_GalletaDulce.IsChecked)
                preferencias.Add("Galleta: Dulce");
            else if (Rb_GalletaSalada.IsChecked)
                preferencias.Add("Galleta: Salada");
            else
                preferencias.Add("Galleta: No seleccionada");

            // Validar sabor
            if (Rb_cafe.IsChecked)
                preferencias.Add("Sabor: Café");
            else if (Rb_chocolate.IsChecked)
                preferencias.Add("Sabor: Chocolate");
            else
                preferencias.Add("Sabor: No seleccionado");

            Indica.Text = string.Join(" | ", preferencias);

            // Validación con alerta
            if (!chk_colorazul.IsChecked && !chk_colorrojo.IsChecked)
            {
                DisplayAlert("Alerta", "Seleccione al menos un color", "OK");
            }
        }

        private void OnSearchBarTextChanged(object sender, TextChangedEventArgs e)
        {
            string term = e.NewTextValue?.ToLower() ?? "";
            if (!string.IsNullOrEmpty(term))
            {
                Indica.Text = $"Buscando: {term}";
            }
        }
    }
}
