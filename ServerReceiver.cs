using NetMQ;
using NetMQ.Sockets;
using System;
using System.Threading;

namespace ServerReceiver {
    public class ServerReceiver
    {
        private readonly Thread receiveThread;
        private bool running;
        public ServerReceiver()
        {
            // Create New Thread for Bluetooth Sensor Transmission over TCP Socket
            receiveThread = new Thread((object callback) =>
            {
                // Create new Server Socket connected to port 5555 (The Bluetooth Side is the Client)
                using (var socket = new PullSocket("@tcp://*:5555"))
                {
                    while (running)
                    {
                        // Receive the JSON and pass it back to the calling function
                        string data = socket.ReceiveFrameString();
                        ((Action<string>)callback)(data);
                    }
                }
            });
        }

        // Call this function when starting Sensor Transmission
        public void Start(Action<string> callback)
        {
            running = true;
            receiveThread.Start(callback);
        }

        // Call this function when stopping Sensor Transmission (also kills the thread)
        public void Stop()
        {
            running = false;
            receiveThread.Join();
        }
    }

    // This is the class that you interact with for the Bluetooth Transmission
    public class Server
    {
        private ServerReceiver receiver;
        private string sensor1;
        private string sensor2;

        public Server()
        { }

        // Call this function when starting Bluetooth Transmission
        public void Start()
        {
            receiver = new ServerReceiver();
            receiver.Start((string d) =>
                {
                    // THIS NEEDS TO BE ADJUSTED TO RETURN D (WHICH IS THE JSON) TO SOMEWHERE ELSE
                    if(d.Contains("sensor1"))
                    {
                        SetSensor1(d);
                    }

                    else if (d.Contains("sensor2"))
                    {
                        SetSensor2(d);
                    }
                }
            );
        }

        // Call this function when stopping Bluetooth Transmission
        public void Stop()
        {
            receiver.Stop();
            NetMQConfig.Cleanup();
        }

        // This makes sure to stop the process if not exiting gracefully
        ~Server()
        {
            Stop();
        }

        private void SetSensor1(string inSensor1)
        {
            sensor1 = inSensor1;
        }

        private void SetSensor2(string inSensor2)
        {
            sensor2 = inSensor2;
        }

        public string GetSensor1()
        {
            return sensor1;
        }

        public string GetSensor2()
        {
            return sensor2;
        }
    }

    
}
