/*
 * FILENAME         :   Program.cs
 * PROGRAMMER       :   Jerry Goe
 * DATE             :   2020/10/19 
 * DESCRIPTION      :   This project is an excercise to work with data structures, delagates, threads and mutexes. We are to create mutiple threads that will be executed and timed with checking
 *                      the size of a file that we create in this console application. Once we create the file, we will get the max size of the file and then the max threads to be used to run in
 *                      the program. When we've gotten all the inputs, we will then append random strings about the size of 100 - 200 characters and then monitor the file every second to see if it
 *                      changed the file size of the file. When its done we close the threads gracefully using the Join() thread method.
*/
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace A_04 {
    class Program {

        const int MIN_FILE_SIZE = 1000;
        const int MAX_FILE_SIZE = 2000000;
        const int MIN_THREAD_COUNT = 1;
        const int MAX_THREAD_COUNT = 1000;
        const int MAX_CHAR_COUNT = 100;

        static Mutex myMutex;
        static volatile bool isItRunning = true;
        
        static void Main(string[] args)
        {

            string fileName = "";
            string maxSizeOfFile = "";
            string threadCount = "";

            int convMaxFileSize = 0; //My converted file size in integer form
            int convThreadCount = 0; //My converted thread count in integer form

            bool isInteger = false;
            bool isThreadCountValid = false;

            if (!Mutex.TryOpenExisting("SpecialMutex", out myMutex))    // proper mutex checking and creation
            {
                myMutex = new Mutex(true, "SpecialMutex");
                myMutex.ReleaseMutex();
            }

            //File 
            Console.WriteLine("What is the name of the file you want to write to: ");
            while (true)
            {
                fileName = Console.ReadLine() + ".txt";
                if (fileName == ".txt")
                {
                    Console.WriteLine(fileName + " cannot be blank, please try again...");
                }
                else
                {
                    break;
                }
            }

            StreamWriter newFile = File.CreateText(fileName);
            newFile.Close();


            Console.WriteLine("What is the maximum size of this file (1000 - 2000000 bytes): ");
            // This will dictate how many strings will be written to this file
            // Use StringBuilder to generate random strings to the file
            while (isInteger != true)
            {
                maxSizeOfFile = Console.ReadLine();
                bool changedToInteger = Int32.TryParse(maxSizeOfFile, out convMaxFileSize);
                if (changedToInteger == true)
                {
                    if (ValidateFileSize(convMaxFileSize) != 0)
                    {
                        isInteger = true;
                    }
                    else
                    {
                        Console.WriteLine("Your max file size was not between {0} and {1}, please try again....", MIN_FILE_SIZE, MAX_FILE_SIZE);
                        isInteger = false;
                    }
                }
                else
                {
                    Console.WriteLine("Could not convert from string to integer, please try again...");
                    isInteger = false;
                }
            }

            Console.WriteLine("How many threads do you want to create for the write operation (1 - 1000 threads): ");
            while (isThreadCountValid != true)
            {
                threadCount = Console.ReadLine();
                bool threadCountConverted = Int32.TryParse(threadCount, out convThreadCount);
                if (threadCountConverted == true)
                {
                    if (ValidateThreadCount(convThreadCount) != 0)
                    {
                        isThreadCountValid = true;
                    }
                    else
                    {
                        Console.WriteLine("Your thread count was not between {0} and {1}, please try again....", MIN_THREAD_COUNT, MAX_THREAD_COUNT);
                        isThreadCountValid = false;
                    }
                }
                else
                {
                    Console.WriteLine("Could not convert from string to integer, please try again...");
                    isThreadCountValid = false;
                }
            }

            //Create a list that will contain all created threads
            List<Thread> threadList = new List<Thread>(); //List to hold my threads

            //Create your threads and add them to a list
            for (int i = 0; i < convThreadCount; i++)
            {
                Thread writeThread = new Thread(new ParameterizedThreadStart(WriteToFile));
                writeThread.Name = "Thread " + i;
                threadList.Add(writeThread);
            }

            foreach (Thread myThreads in threadList)
            {
                myThreads.Start(fileName);
            }

            Thread monitorThread = new Thread(() => MonitorFileSize(fileName, convMaxFileSize));
            monitorThread.Name = "Monitor";
            monitorThread.Start();
            
            foreach (Thread myThreads in threadList)
            {
                myThreads.Join();
            }
            monitorThread.Join();


            Console.WriteLine("Program Completed: Press any key to exit...");
            Console.ReadKey();
        }

        /*
         * FUNCTION         :   ValidateFileSize
         * DESCRIPTION      :   This method is to check the size of what the user entered. If it is between the range of 2000 to 2000000
         *                      If it isn't then return 0 and it will then give an appropriate error message, if not, then return the file size value
         * 
         */
        public static int ValidateFileSize(int myFileSize)
        {
            if (myFileSize < MIN_FILE_SIZE || myFileSize > MAX_FILE_SIZE)
            {
                return 0;
            }
            else
            {
                return myFileSize;
            }
        }

        /*
         * FUNCTION         :   ValidateThreadCount
         * DESCRIPTION      :   This method is to check the number of threads that will be created and sent to a list. Like checking the
         *                      file size, this will return 0 and prompt an error message, if not then it will give the value of the thread
         *                      count
         * 
         */
        public static int ValidateThreadCount(int myThreadCount)
        {
            if (myThreadCount < MIN_THREAD_COUNT || myThreadCount > MAX_THREAD_COUNT)
            {
                return 0;
            }
            else
            {
                return myThreadCount;
            }
        }

        /*
         * FUNCTION         :   MonitorFileSize
         * DESCRIPTION      :   This method is meant to monitor the size of the file using threads. 
         *                      
         * 
         */
        static void MonitorFileSize(object myFileName, object myFileSize)
        {
            string fileName = (string)myFileName;
            int maxFileSize = (int)myFileSize;


            while (isItRunning == true)
            {
                lock (myMutex)
                {
                    FileInfo sizeOfFile = new FileInfo(fileName); //Get the file info for file
                    if (sizeOfFile.Length >= maxFileSize)
                    {
                        Console.WriteLine("Final size: {0}", sizeOfFile.Length);
                        isItRunning = false;
                        break;
                    }
                    Console.WriteLine("Size of " + fileName + " is " + sizeOfFile.Length);
                    Thread.Sleep(1000);
                }
                
            }
        }

        /*
         * FUNCTION         :   WriteToFile
         * DESCRIPTION      :   This method is to write random strings to a file that the user specified. With a specified sized string.
         *                     
         * 
         */
        static void WriteToFile(object fileName)
        {

            StreamWriter myFile = null;
            string badResult = "";

            string strFileName = (string)fileName;
            StringBuilder myString = new StringBuilder();

            Random rdm = new Random();
            int stringLength = MAX_CHAR_COUNT;
            char myLetters;

            Thread checkThread = Thread.CurrentThread;

            for (int i = 0; i <= stringLength; i++)
            {
                //https://www.educative.io/edpresso/how-to-generate-a-random-string-in-c-sharp
                //https://docs.microsoft.com/en-us/dotnet/api/system.random.nextdouble?view=netcore-3.1
                double randFlt = rdm.NextDouble(); //NextDouble is a value that is between 0.0 to 1.0 inclusive
                int randInt = Convert.ToInt32(Math.Floor(25 * randFlt)); //Get a random number between 0 to 25 inclusive
                myLetters = Convert.ToChar(randInt + 65); //get a random character A - Z ASCII index
                myString.Append(myLetters); //Make the string of 130 characters
            }


            while (isItRunning == true)
            {
                lock (myMutex)
                {
                    try
                    {
                        //myMutex.WaitOne();
                        myFile = new StreamWriter(strFileName, true);
                        myFile.WriteLine(myString);
                    }
                    catch (Exception err)
                    {
                        badResult = "Exception: " + err.Message;
                        Console.WriteLine(badResult);

                    }
                    finally
                    {
                        if (myFile != null)
                        {
                            myFile.Close();
                            myFile = null;
                        }

                    }
                    
                    Thread.Sleep(100);
                }
            }
        }
    }
}

