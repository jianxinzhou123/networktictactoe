//Author: Silin Chen
//        Jianxin Zhou
//CSE 483

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// added for debug output
using System.Diagnostics;

using SocketSetup;

namespace TicTacToe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Model _model;

        public MainWindow()
        {
            InitializeComponent();
            
            // make it so the user cannot resize the window
            this.ResizeMode = ResizeMode.NoResize;

            // create an instance of our Model
            _model = new Model();

            Play.IsEnabled = false;

            //set data binding context to our model
            DataContext = _model;

            // create an observable collection. this collection
            // contains the tiles the represent the Tic Tac Toe grid
            MyItemsControl.ItemsSource = _model.TileCollection;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string temp = ((Button)sender).Name;
            if (temp == "Play")
            {
                temp = _model.UserSelection(temp);
            }
            else
            {
                var selectedButton = e.OriginalSource as FrameworkElement;
                if (selectedButton != null)
                {
                    var currentTile = selectedButton.DataContext as Tile;
                    temp = _model.UserSelection(currentTile.TileName);
                }
            }
        }

        private void SetUp_Click(object sender, RoutedEventArgs e)
        {
            SocketSetupWindow socketSetupWindow = new SocketSetupWindow();
            socketSetupWindow.ShowDialog();

            SetUp.IsEnabled = false;
            Play.IsEnabled = true;

            _model.SetLocalNetworkSettings(socketSetupWindow.SocketData.LocalPort, socketSetupWindow.SocketData.LocalIPString);
            _model.SetRemoteNetworkSettings(socketSetupWindow.SocketData.RemotePort, socketSetupWindow.SocketData.RemoteIPString);

            // initialize model and get the ball rolling
            _model.InitModel();
            // set title bar to be unique
            this.Title = this.Title + " " + socketSetupWindow.SocketData.LocalIPString + "@" + socketSetupWindow.SocketData.LocalPort.ToString();
        }
    }
}
