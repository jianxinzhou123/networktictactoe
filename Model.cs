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
using System.Windows.Media;

// Sockets
using System.Net.Sockets;
using System.Net;

// debug
using System.Diagnostics;

// threading
using System.Threading;

// byte data serialization
using System.Runtime.Serialization.Formatters.Binary;

// memory streams
using System.IO;

namespace TicTacToe
{
    public class Model : INotifyPropertyChanged
    {

        [Serializable]
        struct GameData
        {
            public String message;
            public String status;

            public GameData(String msg, String sts)
            {
                status = sts;
                message = msg;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private String _statusTextBox = "Please set up both windows before start!";
        public String StatusTextBox
        {
            get { return _statusTextBox; }
            set
            {
                _statusTextBox = value;
                OnPropertyChanged("StatusTextBox");
            }
        }

        // this is the UDP socket that will be used to communicate
        // over the network
        UdpClient _dataSocket;

        // some data that keeps track of ports and addresses
        private static UInt32 _localPort;
        private static String _localIPAddress;
        private static UInt32 _remotePort;
        private static String _remoteIPAddress;

        // this is the thread that will run in the background
        // waiting for incomming data
        private Thread _receiveDataThread;

        // this thread is used to synchronize the startup of 
        // two UDP peers
        private Thread _synchWithOtherPlayerThread;

        public ObservableCollection<Tile> TileCollection;
        private bool _isX = true;
        private bool _gameIsOver = false;
        private readonly UInt32 _numTiles = 9;

        private String receivedData;
        private String receivedStatus;

        private bool hasSetup = false;

        private bool waitForTurn = false;


        /// <summary>
        /// this method is called to set this UDP peer's local port and address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ipAddress"></param>
        public void SetLocalNetworkSettings(UInt32 port, String ipAddress)
        {
            _localPort = port;
            _localIPAddress = ipAddress;
        }

        /// <summary>
        /// this method is called to set the remote UDP peer's port and address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="ipAddress"></param>
        public void SetRemoteNetworkSettings(UInt32 port, String ipAddress)
        {
            _remotePort = port;
            _remoteIPAddress = ipAddress;
        }

        /// <summary>
        /// Model constructor
        /// </summary>
        /// <returns></returns>
        public Model()
        {
            TileCollection = new ObservableCollection<Tile>();
            for (int i = 0; i < _numTiles; i++)
            {
                TileCollection.Add(new Tile() { TileBrush = Brushes.Black, TileLabel = "", TileName = i.ToString() });
            }
        }

        public bool InitModel()
        {
            try
            {
                // set up generic UDP socket and bind to local port
                //
                _dataSocket = new UdpClient((int)_localPort);
            }
            catch (Exception ex)
            {
                Debug.Write(ex.ToString());
                return false;
            }

            ThreadStart threadFunction;
            threadFunction = new ThreadStart(SynchWithOtherPlayer);
            _synchWithOtherPlayerThread = new Thread(threadFunction);
            Console.WriteLine(DateTime.Now + ":" + " Waiting for other UDP peer to join.\n");
            StatusTextBox = "Waiting for another player";
            _synchWithOtherPlayerThread.Start();

            if (_localPort > _remotePort)
            {
                waitForTurn = false;
                StatusTextBox += "\nYou are Player 1!";
                hasSetup = true;
            }
            else
            {
                hasSetup = true;
                waitForTurn = true;
                StatusTextBox += "\nYou are Player 2!";
            }

            return true;
        }

        /// <summary>
        /// processes all buttons. called from view when a button is clicked
        /// </summary>
        /// <param name="buttonSelected"></param>
        /// <returns></returns>
        public string UserSelection(String buttonSelected)
        {
            if (hasSetup)
            {
                Debug.Write("Button selected was " + buttonSelected + "\n");

                // special processing for named buttons
                if (buttonSelected == "Play")
                {

                    GameData gameData;

                    // formatter used for serialization of data
                    BinaryFormatter formatter = new BinaryFormatter();

                    // stream needed for serialization
                    MemoryStream stream = new MemoryStream();

                    // Byte array needed to send data over a socket
                    Byte[] sendBytes;

                    gameData.message = buttonSelected;// serialize the gameData structure to a stream

                    if (_localPort > _remotePort)
                    {
                        gameData.status = "Player 1 has choose to restart!";
                        StatusTextBox = gameData.status;
                    }
                    else
                    {
                        gameData.status = "Player 2 has choose to restart!";
                        StatusTextBox = gameData.status;
                    }

                    formatter.Serialize(stream, gameData);

                    // retrieve a Byte array from the stream
                    sendBytes = stream.ToArray();

                    // send the serialized data
                    IPEndPoint remoteHost = new IPEndPoint(IPAddress.Parse(_remoteIPAddress), (int)_remotePort);

                    _dataSocket.Send(sendBytes, sendBytes.Length, remoteHost);

                    return RestartGame();
                }
                // process buttons 0-9 in the grid
                else
                {

                    if (!waitForTurn)
                    {
                        // data structure used to communicate data with the other player
                        GameData gameData;

                        // formatter used for serialization of data
                        BinaryFormatter formatter = new BinaryFormatter();

                        // stream needed for serialization
                        MemoryStream stream = new MemoryStream();

                        // Byte array needed to send data over a socket
                        Byte[] sendBytes;

                        gameData.message = buttonSelected;// serialize the gameData structure to a stream

                        if (_localPort > _remotePort)
                        {
                            gameData.status = "Player 1 has selected button " + (int.Parse(buttonSelected));
                            StatusTextBox = gameData.status;
                        }
                        else
                        {
                            gameData.status = "Player 2 has selected button " + (int.Parse(buttonSelected));
                            StatusTextBox = gameData.status;
                        }

                        formatter.Serialize(stream, gameData);

                        // retrieve a Byte array from the stream
                        sendBytes = stream.ToArray();

                        // send the serialized data
                        IPEndPoint remoteHost = new IPEndPoint(IPAddress.Parse(_remoteIPAddress), (int)_remotePort);

                        _dataSocket.Send(sendBytes, sendBytes.Length, remoteHost);

                        waitForTurn = true;

                        int index = int.Parse(buttonSelected);
                        return userSelectionHelper(index);
                    }

                    return null;
                }
            }
            return null;
        }

        /// <summary>
        /// UserSelection helper
        /// </summary>
        /// <param name="buttonIndex"></param>
        /// <returns></returns>
        private string userSelectionHelper(int buttonIndex)
        {
            if (_gameIsOver)
            {
                StatusTextBox = "Game is over.\n Hit Play to restart!";

                // data structure used to communicate data with the other player
                GameData gameData;

                // formatter used for serialization of data
                BinaryFormatter formatter = new BinaryFormatter();

                // stream needed for serialization
                MemoryStream stream = new MemoryStream();

                // Byte array needed to send data over a socket
                Byte[] sendBytes;

                gameData.status = StatusTextBox;
                gameData.message = "";

                formatter.Serialize(stream, gameData);

                // retrieve a Byte array from the stream
                sendBytes = stream.ToArray();

                // send the serialized data
                IPEndPoint remoteHost = new IPEndPoint(IPAddress.Parse(_remoteIPAddress), (int)_remotePort);

                _dataSocket.Send(sendBytes, sendBytes.Length, remoteHost);

                return "Game is over.\n Hit Play to restart!";
            }

            if (TileCollection[buttonIndex].TileLabel != "")	// if is it empty
            {
                return "Try Again!";
            }

            setUserXorO(buttonIndex);

            if (CheckAndProcessWinner() == true)
            {
                _gameIsOver = true;

                StatusTextBox = "We have a winner!";

                // data structure used to communicate data with the other player
                GameData gameData;

                // formatter used for serialization of data
                BinaryFormatter formatter = new BinaryFormatter();

                // stream needed for serialization
                MemoryStream stream = new MemoryStream();

                // Byte array needed to send data over a socket
                Byte[] sendBytes;

                gameData.status = StatusTextBox;
                gameData.message = "";

                formatter.Serialize(stream, gameData);

                // retrieve a Byte array from the stream
                sendBytes = stream.ToArray();

                // send the serialized data
                IPEndPoint remoteHost = new IPEndPoint(IPAddress.Parse(_remoteIPAddress), (int)_remotePort);

                _dataSocket.Send(sendBytes, sendBytes.Length, remoteHost);

                return "We have a Winner!";
            }

            _isX = !_isX;	// prepare for next character

            //           this._isGameOver = TicTacToeUtils.CheckAndProcessWinner(_buttonArray);

            return null;

        }

        /// <summary>
        /// resets all tiles and game state
        /// </summary>
        /// <returns></returns>
        public String RestartGame()
        {
            for (int x = 0; x < _numTiles; x++)
            {
                TileCollection[x].TileLabel = "";
                TileCollection[x].TileBrush = Brushes.Black;
            }

            _gameIsOver = false;
            _isX = true;
            InitModel();
            return "X Goes First!";
        }

        private void setUserXorO(int number)
        {
            if (_isX)
                TileCollection[number].TileLabel = "X";
            else
                TileCollection[number].TileLabel = "O";

        }

        // Winners contains all the array locations of
        // the winning combination -- if they are all 
        // either X or O (and not blank)
        private int[,] Winners = new int[,]
				   {
						{0,1,2},
						{3,4,5},
						{6,7,8},
						{0,3,6},
						{1,4,7},
						{2,5,8},
						{0,4,8},
						{2,4,6}
				   };

        /// <summary>
        /// CheckAndProcessWinner determines if either X or O has won.
        /// Once a winner has been determined, play stops.
        /// </summary>
        /// <returns></returns>
        private bool CheckAndProcessWinner()
        {
            bool gameOver = false;
            for (int i = 0; i < 8; i++)
            {
                int a = Winners[i, 0], b = Winners[i, 1], c = Winners[i, 2];		// get the indices
                // of the winners


                if (TileCollection[a].TileLabel == "" || TileCollection[b].TileLabel == "" || TileCollection[c].TileLabel == "")	// any of the squares blank
                    continue;											// try another -- no need to go further

                if (TileCollection[a].TileLabel == TileCollection[b].TileLabel && TileCollection[b].TileLabel == TileCollection[c].TileLabel)			// are they the same?
                {														// if so, they WIN!
                    gameOver = true;
                    for (int x = 0; x < _numTiles; x++)
                        TileCollection[x].TileBrush = Brushes.Gray;
                    setWinnerGridColor(a);
                    setWinnerGridColor(b);
                    setWinnerGridColor(c);
                    break;  // don't bother to continue
                }
            }
            return gameOver;
        }

        /// <summary>
        /// sets foreground color of all tiles in winning row to make it stand out
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private void setWinnerGridColor(int index)
        {
            TileCollection[index].TileBrush = Brushes.Red;
        }

        private void SynchWithOtherPlayer()
        {

            // set up socket for sending synch byte to UDP peer
            // we can't use the same socket (i.e. _dataSocket) in the same thread context in this manner
            // so we need to set up a separate socket here
            Byte[] data = new Byte[1];
            IPEndPoint endPointSend = new IPEndPoint(IPAddress.Parse(_remoteIPAddress), (int)_remotePort);
            IPEndPoint endPointRecieve = new IPEndPoint(IPAddress.Any, 0);

            UdpClient synchSocket = new UdpClient((int)_localPort + 10);

            // set timeout of receive to 1 second
            _dataSocket.Client.ReceiveTimeout = 1000;

            while (true)
            {
                try
                {
                    synchSocket.Send(data, data.Length, endPointSend);
                    _dataSocket.Receive(ref endPointRecieve);

                    // got something, so break out of loop
                    break;
                }
                catch (SocketException ex)
                {
                    // we get an exception if there was a timeout
                    // if we timed out, we just go back and try again
                    if (ex.ErrorCode == (int)SocketError.TimedOut)
                    {
                        Debug.Write(ex.ToString());
                    }
                    else
                    {
                        // we did not time out, but got a really bad 
                        // error
                        synchSocket.Close();
                        Console.WriteLine("Socket exception occurred. Unable to sync with other UDP peer.\n");
                        return;
                    }
                }
                catch (System.ObjectDisposedException ex)
                {
                    // something bad happened. close the socket and return
                    Console.WriteLine(ex.ToString());
                    synchSocket.Close();
                    return;
                }

            }

            // send synch byte
            synchSocket.Send(data, data.Length, endPointSend);

            // close the socket we used to send periodic requests to player 2
            synchSocket.Close();

            // reset the timeout for the dataSocket to infinite
            // _dataSocket will be used to recieve data from other UDP peer
            _dataSocket.Client.ReceiveTimeout = 0;

            // start the thread to listen for data from other UDP peer
            ThreadStart threadFunction = new ThreadStart(ReceiveThreadFunction);
            _receiveDataThread = new Thread(threadFunction);
            _receiveDataThread.Start();
            StatusTextBox = StatusTextBox + "\nOther player has joined the session.\nHave Fun!";
        }

        private void ReceiveThreadFunction()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            while (true)
            {
                try
                {
                    // wait for data
                    Byte[] receiveData = _dataSocket.Receive(ref endPoint);

                    // check to see if this is synchronization data 
                    // ignore it. we should not recieve any sychronization
                    // data here, because synchronization data should have 
                    // been consumed by the SynchWithOtherPlayer thread. but, 
                    // it is possible to get 1 last synchronization byte, which we
                    // want to ignore
                    if (receiveData.Length < 2)
                        continue;


                    // process and display data


                    GameData gameData;
                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream stream = new MemoryStream();

                    // deserialize data back into our GameData structure
                    stream = new System.IO.MemoryStream(receiveData);
                    gameData = (GameData)formatter.Deserialize(stream);

                    // update view data through our bound properties
                    receivedData = gameData.message;
                    receivedStatus = gameData.status;


                    if (receivedData == "Play")
                    {
                        RestartGame();
                        StatusTextBox = receivedStatus;
                    }
                    else if (receivedData == "")
                    {
                        StatusTextBox = receivedStatus;
                    }
                    else
                    {
                        waitForTurn = false;

                        int index = int.Parse(receivedData);
                        userSelectionHelper(index);

                        StatusTextBox = receivedStatus;

                        // update status window
                        Console.WriteLine(DateTime.Now + ":" + " New message received.\n");
                    }

                }
                catch (SocketException ex)
                {
                    // got here because either the Receive failed, or more
                    // or more likely the socket was destroyed by 
                    // exiting from the JoystickPositionWindow form
                    Console.WriteLine(ex.ToString());
                    return;
                }
                catch (Exception ex)
                {
                    Console.Write(ex.ToString());
                }

            }
        }
    }
}
