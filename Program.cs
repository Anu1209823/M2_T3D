using System;
using System.Collections.Generic;
using System.Threading;

// Structure to hold traffic signal data
public class TrafficSignalData
{
    public string Timestamp { get; set; }
    public string LightId { get; set; }
    public int CarsPassed { get; set; }
}

// Bounded buffer to store traffic signal data
public class BoundedBuffer
{
    private int maxSize;
    private Queue<TrafficSignalData> buffer = new Queue<TrafficSignalData>();
    private object lockObj = new object();

    public BoundedBuffer(int size)
    {
        maxSize = size;
    }

    public void Put(TrafficSignalData data)
    {
        lock (lockObj)
        {
            while (buffer.Count >= maxSize)
            {
                Monitor.Wait(lockObj);
            }
            buffer.Enqueue(data);
            Monitor.Pulse(lockObj);
        }
    }

    public TrafficSignalData Get()
    {
        lock (lockObj)
        {
            while (buffer.Count == 0)
            {
                Monitor.Wait(lockObj);
            }
            TrafficSignalData data = buffer.Dequeue();
            Monitor.Pulse(lockObj);
            return data;
        }
    }
}

public class Program
{
    // Traffic producer function
    public static void TrafficProducer(int id, BoundedBuffer buffer)
    {
        Random rand = new Random();
        while (true)
        {
            string timestamp = DateTime.Now.ToString();
            TrafficSignalData data = new TrafficSignalData
            {
                Timestamp = timestamp,
                LightId = "Signal_" + id,
                CarsPassed = rand.Next(1, 101)
            };
            buffer.Put(data);
            Thread.Sleep(300000); // Sleep for 5 minutes (300,000 milliseconds)
        }
    }

    // Traffic consumer function
    public static void TrafficConsumer(int topN, BoundedBuffer buffer)
    {
        while (true)
        {
            List<TrafficSignalData> trafficData = new List<TrafficSignalData>();
            for (int i = 0; i < 12; i++)
            {
                trafficData.Add(buffer.Get());
            }

            trafficData.Sort((a, b) => b.CarsPassed.CompareTo(a.CarsPassed));

            Console.WriteLine($"Top {topN} congested signals:");
            for (int i = 0; i < Math.Min(topN, trafficData.Count); i++)
            {
                Console.WriteLine($"Light ID: {trafficData[i].LightId}, Cars Passed: {trafficData[i].CarsPassed}");
            }
            Console.WriteLine();

            Thread.Sleep(3600000); // Sleep for 1 hour (3,600,000 milliseconds)
        }
    }

    public static void Main(string[] args)
    {
        Console.WriteLine("Enter the number of traffic signals: ");
        int numSignals = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter the size of the buffer: ");
        int bufferSize = int.Parse(Console.ReadLine());

        Console.WriteLine("Enter the value of N for top N congested signals: ");
        int topN = int.Parse(Console.ReadLine());

        BoundedBuffer buffer = new BoundedBuffer(bufferSize);

        // Create producer threads
        List<Thread> producerThreads = new List<Thread>();
        for (int i = 0; i < numSignals; i++)
        {
            Thread producerThread = new Thread(() => TrafficProducer(i, buffer));
            producerThread.Start();
            producerThreads.Add(producerThread);
        }

        // Create consumer thread
        Thread consumerThread = new Thread(() => TrafficConsumer(topN, buffer));
        consumerThread.Start();

        // Join all threads
        foreach (Thread thread in producerThreads)
        {
            thread.Join();
        }
        consumerThread.Join();
    }
}
