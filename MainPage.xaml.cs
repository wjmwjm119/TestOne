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

using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;

using System.Diagnostics;
using System.Threading.Tasks;

//using DisplayFont;
using WJMIOT;



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






        private GpioController gpioController;
        string oledOutputInfo;


        /////////////////////////////////
        /////////////////////////////////
        /////////////////////////////////



        private GpioPin pinLed;
        private GpioPin pinKeyOne;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;


        /////////////////////////////////
        /////////////////////////////////
        /////////////////////////////////
        // specify which GPIO pins are wired to the distance sensor
        private const int Trig_Pin = 23;
        private const int Echo_Pin = 24;
        private GpioPin trig;
        private GpioPin echo;

        private const int voiceDete_Pin = 27;
        private GpioPin voiceDete;

        // stopwatch to time the echo on the distance sensor
        Stopwatch sw = new Stopwatch();

        // duration of the echo
        TimeSpan elapsedTime;

        // distance between the rover and an obstacle
        double distanceToObstacle;
        bool isCheckDistance;

        

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////////////////////////////////////////////////////////
        ////SPI Display///////////////////////////////////////////////////////////////////////////////////////////////////
        /* Uncomment for Raspberry Pi 2 */
        private const string SPI_CONTROLLER_NAME = "SPI0";  /* For Raspberry Pi 2, use SPI0                             */
        private const Int32 SPI_CHIP_SELECT_LINE = 0;       /* Line 0 maps to physical pin number 24 on the Rpi2        */
        private const Int32 DATA_COMMAND_PIN = 22;          /* We use GPIO 22 since it's conveniently near the SPI pins */
        private const Int32 RESET_PIN = 23;                 /* We use GPIO 23 since it's conveniently near the SPI pins */

        /* This sample is intended to be used with the following OLED display: http://www.adafruit.com/product/938 */
        private const UInt32 SCREEN_WIDTH_PX = 132;                         /* Number of horizontal pixels on the display */
        private const UInt32 SCREEN_HEIGHT_PX = 64;                         /* Number of vertical pixels on the display   */
        private const UInt32 SCREEN_HEIGHT_PAGES = SCREEN_HEIGHT_PX / 8;    /* The vertical pixels on this display are arranged into 'pages' of 8 pixels each */
        byte[] tempUpBuffer = new byte[SCREEN_WIDTH_PX];
        byte[] tempDownBuffer = new byte[SCREEN_WIDTH_PX];

        private byte[] SerializedDisplayBuffer = new byte[SCREEN_WIDTH_PX * SCREEN_HEIGHT_PAGES];                /* A temporary buffer used to prepare graphics data for sending over SPI          */

        /* Definitions for SPI and GPIO */
        private SpiDevice spiDevice0;
        private GpioPin DataCommandPin;
        private GpioPin ResetPin;

        //nrf24L01P
        //1:GND 2:VCC 3:CE 4:CSN 5:SCK 6:MOSI 7:MISO 8:IRQ
        //P7:4:CSN    P6:25:IRQ

        private SpiDevice spiDevice1;
        private GpioPin nrf_CSN_Pin;
        private GpioPin nrf_IRQ_Pin;


        public MainPage()
        {

            this.InitializeComponent();

            /*
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += Timer_Tick;

            if (InitGPIO_Wave())
            {
                timer.Start();
            }
            */
            //      InitNRF24L01P();
            InitOledDisplay();

        }


//        async void MainTask()
 //       {
            

//        }





        ////////////////////////////////
        /////////////////////////////////////////////////////////////
        /////////////////////////////


            


        private async void InitOledDisplay()
        {
            try
            {
                InitGpio_Display();             /* Initialize the GPIO controller and GPIO pins */
                 await InitSpi0();        /* Initialize the SPI controller                */
                 await InitDisplayRegister();    /* Initialize the display                       */


            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                if (ex.InnerException != null)
                {
                    Debug.WriteLine("\nInner Exception: " + ex.InnerException.Message);
                }
                return;
            }

            /* Register a handler so we update the SPI display anytime the user edits a textbox */
            // Display_TextBoxLine0.TextChanged += Display_TextBox_TextChanged;


            oledOutputInfo += " OLED Init! ";
            DisplayString(oledOutputInfo);


            InitNRF24L01P();


        }

        private void InitGpio_Display()
        {
            gpioController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */
            if (gpioController == null)
            {
                throw new Exception("GPIO does not exist on the current system.");
            }

            //A0
            /* Initialize a pin as output for the Data/Command line on the display  */
            DataCommandPin = gpioController.OpenPin(DATA_COMMAND_PIN);
            DataCommandPin.Write(GpioPinValue.High);
            DataCommandPin.SetDriveMode(GpioPinDriveMode.Output);

            //
            /* Initialize a pin as output for the hardware Reset line on the display */
            ResetPin = gpioController.OpenPin(RESET_PIN);
            ResetPin.Write(GpioPinValue.High);
            ResetPin.SetDriveMode(GpioPinDriveMode.Output);

        }

        private async Task InitSpi0()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE); /* Create SPI initialization settings                               */
                settings.ClockFrequency = 10000000;                             /* Datasheet specifies maximum SPI clock frequency of 10MHz         */
                settings.Mode = SpiMode.Mode3;                                  /* The display expects an idle-high clock polarity, we use Mode3    
                                                                                 * to set the clock polarity and phase to: CPOL = 1, CPHA = 1        */


                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);       /* Find the selector string for the SPI bus controller          */
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);         /* Find the SPI bus controller device with our selector string  */
                spiDevice0 = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);  /* Create an SpiDevice with our bus controller and SPI settings */

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }

        private async Task InitDisplayRegister()
        {
            /* Initialize the display */
            try
            {
                ResetPin.Write(GpioPinValue.Low);   /* Put display into reset                       */
                await Task.Delay(10);                /* Wait at least 3uS (We wait 1mS since that is the minimum delay we can specify for Task.Delay() */
                ResetPin.Write(GpioPinValue.High);  /* Bring display out of reset                   */
                await Task.Delay(100);              /* Wait at least 100mS before sending commands  */

                ClearScreen();

                DataCommandPin.Write(GpioPinValue.Low);
                spiDevice0.Write(new byte[] { 0x10 });//输入返回第一行
                spiDevice0.Write(new byte[] { 0x00 });//输入返回第一行
                spiDevice0.Write(new byte[] { 0xA0 });
                spiDevice0.Write(new byte[]{0xAF});     /* Turn the display on                                                      */
                DataCommandPin.Write(GpioPinValue.High);


 
                  //        DisplayString("  6gdf Display Initialization Failed  DataCommandPin  microprocessor interface as an example ");
                //                await Task.Delay(2000);              /* Wait at least 100mS before sending commands  */

                DisplayString("abcdefghijklmnopqrstuvwxyz{|}~");

            }
            catch (Exception ex)
            {
                throw new Exception("Display Initialization Failed", ex);
            }
        }

        void ClearScreen()
        {
            Array.Clear(SerializedDisplayBuffer, 0, SerializedDisplayBuffer.Length);

            
            for (int i = 0; i < SCREEN_HEIGHT_PAGES; i++)
            {
                //0x0B0 page adress
                int page=i+176;

                DataCommandPin.Write(GpioPinValue.Low);
                spiDevice0.Write(BitConverter.GetBytes(page));
                DataCommandPin.Write(GpioPinValue.High);

                spiDevice0.Write(SerializedDisplayBuffer);
            }
        }

        void DisplayString(string str)
        {
            ClearScreen();
            int lineCount=0;//两个page拼成一行
            int currentPixeWidth=0;

            Array.Clear(tempUpBuffer, 0, tempUpBuffer.Length);//清空缓存
            Array.Clear(tempDownBuffer, 0, tempDownBuffer.Length);//清空缓存

            FontCharacterDescriptor[] fontDesGroup = new FontCharacterDescriptor[str.Length];


            for (int i = 0; i < str.Length; i++)
            {
                fontDesGroup[i] = DisplayFontTable.GetCharacterDescriptor(str[i]);

 //               Debug.WriteLine(fontDesGroup[i].characterDataUp[0].ToString("X"));


                if (currentPixeWidth < 110)
                {
                    Array.Copy(fontDesGroup[i].characterDataUp, 0, tempUpBuffer, currentPixeWidth, fontDesGroup[i].characterWidthPx);
                    Array.Copy(fontDesGroup[i].characterDataDown, 0, tempDownBuffer, currentPixeWidth, fontDesGroup[i].characterWidthPx);
                    currentPixeWidth += fontDesGroup[i].characterWidthPx;
                }
                else
                {
                    Array.Copy(fontDesGroup[i].characterDataUp, 0, tempUpBuffer, currentPixeWidth, fontDesGroup[i].characterWidthPx);
                    Array.Copy(fontDesGroup[i].characterDataDown, 0, tempDownBuffer, currentPixeWidth, fontDesGroup[i].characterWidthPx);

                    DisplayWriteLine(ref tempUpBuffer, lineCount, 0);
                    DisplayWriteLine(ref tempDownBuffer, lineCount, 1);

                    lineCount++;

                    if (lineCount > 3)
                    {
                        break;
                    }

                    currentPixeWidth =0;

                }

               
                if (i == str.Length-1)
                {
                    DisplayWriteLine(ref tempUpBuffer, lineCount, 0);
                    DisplayWriteLine(ref tempDownBuffer, lineCount, 1);
                }

                



            }

        }

        void DisplayWriteLine(ref byte[] buffer,int lineCount,int offset)
        {

            DataCommandPin.Write(GpioPinValue.Low);
            spiDevice0.Write(new byte[] { 0x10 });//输入返回第一行
            spiDevice0.Write(new byte[] { 0x00 });//输入返回第一行
            spiDevice0.Write(BitConverter.GetBytes(176 + lineCount * 2+offset));
            DataCommandPin.Write(GpioPinValue.High);

            spiDevice0.Write(new byte[] { 0x00, 0x00});
            spiDevice0.Write(buffer);

            Array.Clear(buffer, 0, buffer.Length);//清空缓存

        }







////////////////////////////////
/////////////////////////////////////////////////////////////
/////////////////////////////

        private bool InitGPIO_Wave()
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

                voiceDete = gpioController.OpenPin(voiceDete_Pin);
                voiceDete.SetDriveMode(GpioPinDriveMode.Input);
                voiceDete.ValueChanged += VoiceDete;


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

            DistanceReading();

            DisplayString( distanceToObstacle.ToString()+"\n");


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

        private void VoiceDete(GpioPin gpioPin, GpioPinValueChangedEventArgs e)
        {

            if (gpioPin.Read() == GpioPinValue.Low)
            {

                Debug.WriteLine("VoiceDete");

                DistanceReading();

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


            }

        }





        ////////////////////////////////
        /////////////////////////////////////////////////////////////
        /////////////////////////////





        async void InitNRF24L01P()
        {
            InitGPIO_nrf24L01P();
            await InitSpi1();


        }




        //  nrf24L01P无线
        //  正面
        //  8，7
        //  6，5
        //  4，3
        //  2，1

        //1:GND 2:VCC 3:CE 4:CSN 5:SCK 6:MOSI 7:MISO 8:IRQ
        //P7:4:CSN    P6:25:IRQ

        private void  InitGPIO_nrf24L01P()
        {

            gpioController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */
            if (gpioController == null)
            {
                throw new Exception("GPIO does not exist on the current system.");
            }

            //A0
            /* Initialize a pin as output for the Data/Command line on the display  */
            nrf_CSN_Pin = gpioController.OpenPin(4);
            nrf_CSN_Pin.Write(GpioPinValue.High);
            nrf_CSN_Pin.SetDriveMode(GpioPinDriveMode.Output);

            //
            /* Initialize a pin as output for the hardware Reset line on the display */
            nrf_IRQ_Pin = gpioController.OpenPin(25);
            nrf_IRQ_Pin.Write(GpioPinValue.High);
            nrf_IRQ_Pin.SetDriveMode(GpioPinDriveMode.Output);


        }

        private async Task InitSpi1()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE); /* Create SPI initialization settings                               */
                settings.ClockFrequency = 10000000;                             /* Datasheet specifies maximum SPI clock frequency of 10MHz         */
                settings.Mode = SpiMode.Mode3;                                  /* The display expects an idle-high clock polarity, we use Mode3    
                                                                                 * to set the clock polarity and phase to: CPOL = 1, CPHA = 1        */


                string spiAqs = SpiDevice.GetDeviceSelector("SPI1");       /* Find the selector string for the SPI bus controller          */
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);         /* Find the SPI bus controller device with our selector string  */
                spiDevice1 = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);  /* Create an SpiDevice with our bus controller and SPI settings */

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI1 Initialization Failed", ex);
            }

            oledOutputInfo+= " SPI1 Init!!";
 //           oledOutputInfo+= spiDevice1.ConnectionSettings.ToString();

            DisplayString(oledOutputInfo);


        }


        private async Task InitNRF24L01PRegister()
        {
            /* Initialize the display */
            try
            {
     
            }
            catch (Exception ex)
            {
                throw new Exception("NRF24L01P Register Initialization Failed", ex);
            }
        }




    }




}





