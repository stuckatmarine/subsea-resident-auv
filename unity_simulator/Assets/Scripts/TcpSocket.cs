using UnityEngine;
//using System.Net;
using System.Net.Sockets;
using System;
using System.Threading;

public class TcpSocket : MonoBehaviour
{
    public string ip = "";
    public int port = 7001;
    private Socket client;

    public bool connectTcpSocket()
    {
        client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        try
        {
            client.Connect(ip, port);
        }
        catch(SocketException ex)
        {
            Debug.Log(ex.Message);
            return false;
            // Try to reconnect ??  TODO
            Thread.Sleep(10000);
        }

        if (!client.Connected) {
            Debug.LogError("Connection Failed");
            return false; 
        }
        return true;
    }

    /// <summary> 
    /// Send data to port, receive data from port.
    /// </summary>
    /// <param name="dataOut">Data to send</param>
    /// <returns></returns>
    public byte[] SendAndReceive(byte[] dataOut)
    {
        client.Send(dataOut);

        byte[] bytes = new byte[4096];
        int idxUsedBytes = client.Receive(bytes);

        // Debug.Log(System.Text.Encoding.UTF8.GetString(bytes));

        client.Close();
        return bytes;
    }
}