using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.Text;
using CortexAccess;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace MotionLogger
{ 
    public class Program
    {
        const string OutFilePath = @"MotionLogger.csv";
        const string licenseID = ""; // Do not need license id when subscribe motion
        private static FileStream OutFileStream;

        static void Main(string[] args)
        {
            Console.WriteLine("Motion LOGGER");
            Console.WriteLine("Please wear Headset with good signal!!!");
            Console.WriteLine(Screen.PrimaryScreen.Bounds.Width.ToString());
            Console.WriteLine(Screen.PrimaryScreen.Bounds.Height.ToString());
            // Delete Output file if existed
            if (File.Exists(OutFilePath))
            {
                File.Delete(OutFilePath);
            }
            OutFileStream = new FileStream(OutFilePath, FileMode.Append, FileAccess.Write);


            DataStreamExample dse = new DataStreamExample();
            dse.AddStreams("mot");
            dse.OnSubscribed += SubscribedOK;
            dse.OnMotionDataReceived += OnMotionDataReceived;
            dse.Start(licenseID);

            Console.WriteLine("Press Esc to flush data to file and exit");
            while (Console.ReadKey().Key != ConsoleKey.Escape) { }

            // Unsubcribe stream
            dse.UnSubscribe();
            Thread.Sleep(5000);

            // Close Session
            dse.CloseSession();
            Thread.Sleep(5000);
            // Close Out Stream
            OutFileStream.Dispose();
        }

        private static void SubscribedOK(object sender, Dictionary<string, JArray> e)
        {
            foreach (string key in e.Keys)
            {
                if (key == "mot")
                {
                    // print header
                    ArrayList header = e[key].ToObject<ArrayList>();
                    //add timeStamp to header
                    header.Insert(0, "Timestamp");
                    WriteDataToFile(header);
                }
            }
        }

        // Write Header and Data to File
        private static void WriteDataToFile(ArrayList data)
        {
            int i = 0;
            for (; i < data.Count - 1; i++)
            {
                byte[] val = Encoding.UTF8.GetBytes(data[i].ToString() + ", ");

                if (OutFileStream != null)
                    OutFileStream.Write(val, 0, val.Length);
                else
                    break;
            }
            // Last element
            byte[] lastVal = Encoding.UTF8.GetBytes(data[i].ToString() + "\n");
            if (OutFileStream != null)
                OutFileStream.Write(lastVal, 0, lastVal.Length);
        }

        private static void OnMotionDataReceived(object sender, ArrayList motData)
        {
            double[] coordenada_actual = new double[4] { 0, 0, 0, 1 };
            //Console.WriteLine("El q0 es: " + motData[3]);
            //Console.WriteLine("El q1 es: " + motData[4]);
            //Console.WriteLine("El q2 es: " + motData[5]);
            //Console.WriteLine("El q3 es: " + motData[6]);
            //Instancia para poder llamar los metodos
            Program pgrm1 = new Program();
            double[] Vector_casco = new double[4] { (double)motData[3], (double)motData[4], (double)motData[5], (double)motData[6] };
            double[] Vector_Conjugado = pgrm1.conjugate(Vector_casco);
            pgrm1.MoveMouse(coordenada_actual, Vector_casco, Vector_Conjugado); 
            //WriteDataToFile(motData);
        }
        //Multiplica las coordenadas por -1 para obtener su reflexion sobre el eje x
        public double[] conjugate(double[] Vector)
        {
            double[] newVector = new double[4];
            newVector = Vector;
            for (int i=0; i< newVector.Length; i++)
            {
                newVector[i] = newVector[i] * (-1);
            }
            return newVector;
        }
        //Multiplica coordenada por coordenada en forma distributiva
        //Tenemos los vectores en la forma V1 = (a1,b1,c1,d1) y V2 = (a2,b2,c2,d2)
        //Luego para obtener las coordenadas, tenemos por Hamilton que   
        // r = (a1*a2 - b1*b2 - c1*c2 - d1*d2) parte real
        // i = (a1*b2 + b1*a2 + c1*d2 - d1*c2) parte i
        // j = (a1*c2 - b1*d2 + c1*a2 + d1*b2) parte j
        // k = (a1*d2 + b1*c2 - c1*b2 + d1*a2) parte k
        //En este caso le restamos 1 a los indices para coincidirlos con el array
        public double[] HamiltonProduct(double[] Vector1, double[] Vector2)
        {
            double real = Vector1[0] * Vector2[0] - Vector1[1] * Vector2[1] - Vector1[2] * Vector2[2] - Vector1[3] * Vector2[3];
            double i = Vector1[0] * Vector2[1] + Vector1[1] * Vector2[0] + Vector1[2] * Vector2[3] - Vector1[3] * Vector2[2];
            double j = Vector1[0] * Vector2[2] - Vector1[1] * Vector2[3] + Vector1[2] * Vector2[0] + Vector1[3] * Vector2[1];
            double k = Vector1[0] * Vector2[3] + Vector1[1] * Vector2[2] - Vector1[2] * Vector2[1] + Vector1[3] * Vector2[0];

            double[] new_Point = new double[4] {real, i, j, k};
            
            return new_Point;
        }
        [DllImport("User32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        public void MoveMouse(double[] Vector1, double[] Vector2, double[] Vector_Conjugado)
        {
            double[] new_Point = HamiltonProduct(HamiltonProduct(Vector2, Vector1), Vector_Conjugado);
            int X = (int)(Screen.PrimaryScreen.Bounds.Width/2  + (new_Point[2]* Screen.PrimaryScreen.Bounds.Width / 2));
            int Y = (int)(Screen.PrimaryScreen.Bounds.Height/2 + (new_Point[3]* Screen.PrimaryScreen.Bounds.Height / 2));


            Console.WriteLine("x " + X + " y " + Y);
            //Console.WriteLine(new_Point[0] +","+ new_Point[1] + "," + new_Point[2] + "," + new_Point[3]);
            SetCursorPos(X,Y);
        }
    }
    
}