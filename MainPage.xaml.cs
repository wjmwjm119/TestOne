using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Gpio;
using System.Diagnostics;
using System.Threading.Tasks;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestOne
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //DVK512
        //Led Light 26,12,16
        //key 1,2,3 GPIO 5,6,13


        private GpioOpenStatus gpioOpenStatus;
        private GpioController gpioController;
        private GpioPin pinLed;
        private GpioPin pinKeyOne;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;

        // specify which GPIO pins are wired to the distance sensor
        private const int Trig_Pin = 23;
        private const int Echo_Pin = 24;
        private GpioPin trig;
        private GpioPin echo;



        // stopwatch to time the echo on the distance sensor
        Stopwatch sw = new Stopwatch();

        // duration of the echo
        TimeSpan elapsedTime;

        // distance between the rover and an obstacle
        double distanceToObstacle;
        bool isCheckDistance;

        public MainPage()
        {
            this.InitializeComponent();


            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            //   timer.Tick += Timer_Tick;



            if (InitGPIO())
            {



                timer.Start();
            }


        }

        private bool InitGPIO()
        {
            gpioController = GpioController.GetDefault();



            // Show an error if there is no GPIO controller
            if (gpioController == null)
            {

                return false;
            }
            else
            {
                Debug.WriteLine(gpioController.PinCount.ToString());

                pinLed = gpioController.OpenPin(12);
                pinLed.SetDriveMode(GpioPinDriveMode.Output);
                pinLed.Write(GpioPinValue.Low);

                pinKeyOne = gpioController.OpenPin(5);
                pinKeyOne.SetDriveMode(GpioPinDriveMode.InputPullUp);
                pinKeyOne.ValueChanged += KEYoneDown;

                // initialize distance sensor
                trig = gpioController.OpenPin(Trig_Pin);
                echo = gpioController.OpenPin(Echo_Pin);
                trig.SetDriveMode(GpioPinDriveMode.Output);
                echo.SetDriveMode(GpioPinDriveMode.Input);
                trig.Write(GpioPinValue.Low);



                return true;
            }


        }



        private void Timer_Tick(object sender, object e)
        {

            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
            }
            else
            {
                pinValue = GpioPinValue.High;
            }


            pinLed.Write(pinValue);

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("AAA");


        }


        private void KEYoneDown(GpioPin gpioPin, GpioPinValueChangedEventArgs e)
        {


            if (gpioPin.Read() == GpioPinValue.Low)
            {
                Debug.WriteLine("LED ON");
                pinLed.Write(GpioPinValue.High);
                DistanceReading();
            }
            else
            {
                pinLed.Write(GpioPinValue.Low);

            }

        }

        private void DistanceReading()
        {
            if (!isCheckDistance)
            {
                Debug.WriteLine("CheckDistaceStart!");
                isCheckDistance = true;
                // reset the stopwatch
                sw.Reset();

                // ensure the trigger is off
                trig.Write(GpioPinValue.Low);

                // wait for the sensor to settle
                Task.Delay(TimeSpan.FromMilliseconds(500)).Wait();

                // turn on the pulse
                trig.Write(GpioPinValue.High);

                // let the pulse run for 10 microseconds
                Task.Delay(TimeSpan.FromMilliseconds(.01)).Wait();

                // turn off the pulse
                trig.Write(GpioPinValue.Low);

                //start the stopwatch
                sw.Start();

                // wait until the echo starts
                //等待超声波回来，如果收到超声波，Echo端会输出一个高电平，高电平的持续时间为声波传播来回的距离
                while (echo.Read() == GpioPinValue.Low)
                {
                    if (sw.ElapsedMilliseconds > 60)
                    {
                        // if you have waited for more than a second, then there was a failure in the echo

                        isCheckDistance = false;
                        Debug.WriteLine("Distance TimeOut");
                        Debug.WriteLine("CheckDistaceEnd!");
                        break;
                    }
                }


                if (isCheckDistance)
                {
                    //在有效的时间内收到超声波，要重置计算器，以便计算出高电平持续时间
                    sw.Restart();

                    // stop the stopwatch when the echo stops
                    //de
                    while (echo.Read() == GpioPinValue.High) ;
                    sw.Stop();

                    // the duration of the echo is equal to the pulse's roundtrip time
                    elapsedTime = sw.Elapsed;

                    // speed of sound is 34300 cm per second or 34.3 cm per millisecond
                    // since the sound waves traveled to the obstacle and back to the sensor
                    // I am dividing by 2 to represent travel time to the obstacle
                    distanceToObstacle = elapsedTime.TotalMilliseconds * 34.3 / 2.0;
                    isCheckDistance = false;
                    sw.Reset();

                    Debug.WriteLine(elapsedTime.Milliseconds.ToString());
                    Debug.WriteLine(distanceToObstacle.ToString());
                    Debug.WriteLine("CheckDistaceEnd!");

                    isCheckDistance = false;

                }

                Debug.WriteLine("CheckDistaceEnd!");
                ///55555555555555555555555

            }

        }




    }




}





