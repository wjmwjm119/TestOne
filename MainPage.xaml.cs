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

using DisplayFont;




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


        //
        public string ttt;


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
        private const UInt32 SCREEN_WIDTH_PX = 128;                         /* Number of horizontal pixels on the display */
        private const UInt32 SCREEN_HEIGHT_PX = 64;                         /* Number of vertical pixels on the display   */
        private const UInt32 SCREEN_HEIGHT_PAGES = SCREEN_HEIGHT_PX / 8;    /* The vertical pixels on this display are arranged into 'pages' of 8 pixels each */
        private byte[,] DisplayBuffer =
            new byte[SCREEN_WIDTH_PX, SCREEN_HEIGHT_PAGES];                 /* A local buffer we use to store graphics data for the screen                    */
        private byte[] SerializedDisplayBuffer =
            new byte[SCREEN_WIDTH_PX * SCREEN_HEIGHT_PAGES];                /* A temporary buffer used to prepare graphics data for sending over SPI          */

        /* Definitions for SPI and GPIO */
        private SpiDevice SpiDisplay;
        private GpioController IoController;
        private GpioPin DataCommandPin;
        private GpioPin ResetPin;

        /* Display commands. See the datasheet for details on commands: http://www.adafruit.com/datasheets/SSD1306.pdf                      */
        private static readonly byte[] CMD_DISPLAY_OFF = { 0xAE };              /* Turns the display off                                    */
        private static readonly byte[] CMD_DISPLAY_ON = { 0xAF };               /* Turns the display on                                     */
        private static readonly byte[] CMD_CHARGEPUMP_ON = { 0x8D, 0x14 };      /* Turn on internal charge pump to supply power to display  */
        private static readonly byte[] CMD_MEMADDRMODE = { 0x20, 0x00 };        /* Horizontal memory mode                                   */
        private static readonly byte[] CMD_SEGREMAP = { 0xA1 };                 /* Remaps the segments, which has the effect of mirroring the display horizontally */
        private static readonly byte[] CMD_COMSCANDIR = { 0xC8 };               /* Set the COM scan direction to inverse, which flips the screen vertically        */
        private static readonly byte[] CMD_RESETCOLADDR = { 0x21, 0x00, 0x7F }; /* Reset the column address pointer                         */
        private static readonly byte[] CMD_RESETPAGEADDR = { 0x22, 0x00, 0x07 };/* Reset the page address pointer                           */




        public MainPage()
        {

            this.InitializeComponent();

            greetingOutput.Text = "DDD";

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            //   timer.Tick += Timer_Tick;

            /*
            if (InitGPIO())
            {
                timer.Start();
            }
            */


            InitSpiDisplay();

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

                voiceDete = gpioController.OpenPin(voiceDete_Pin);
                voiceDete.SetDriveMode(GpioPinDriveMode.Input);
                voiceDete.ValueChanged+= VoiceDete;


                return true;
            }


        }



        //  nrf24l01无线
        //  正面
        //  8，7
        //  6，5
        //  4，3
        //  2，1

        //1:GND 2:VCC 3:CE 4:CSN 5:SCK 6:MOSI 7:MISO 8:IRQ
        //P3:22:CSN    P6:25:IRQ

        private bool InitGPIO2()
        {

            gpioController = GpioController.GetDefault();



            // Show an error if there is no GPIO controller
            if (gpioController == null)
            {

                return false;
            }
            else
            {
 

    

                return true;
            }


        }


        /* Initialize the GPIO */
        private void InitGpio3()
        {
            IoController = GpioController.GetDefault(); /* Get the default GPIO controller on the system */
            if (IoController == null)
            {
                throw new Exception("GPIO does not exist on the current system.");
            }

            /* Initialize a pin as output for the Data/Command line on the display  */
            DataCommandPin = IoController.OpenPin(DATA_COMMAND_PIN);
            DataCommandPin.Write(GpioPinValue.High);
            DataCommandPin.SetDriveMode(GpioPinDriveMode.Output);

            /* Initialize a pin as output for the hardware Reset line on the display */
            ResetPin = IoController.OpenPin(RESET_PIN);
            ResetPin.Write(GpioPinValue.High);
            ResetPin.SetDriveMode(GpioPinDriveMode.Output);

        }


        private async Task InitSpi()
        {
            try
            {
                var settings = new SpiConnectionSettings(SPI_CHIP_SELECT_LINE); /* Create SPI initialization settings                               */
                settings.ClockFrequency = 10000000;                             /* Datasheet specifies maximum SPI clock frequency of 10MHz         */
                settings.Mode = SpiMode.Mode3;                                  /* The display expects an idle-high clock polarity, we use Mode3    
                                                                                 * to set the clock polarity and phase to: CPOL = 1, CPHA = 1        */


                string spiAqs = SpiDevice.GetDeviceSelector(SPI_CONTROLLER_NAME);       /* Find the selector string for the SPI bus controller          */
                var devicesInfo = await DeviceInformation.FindAllAsync(spiAqs);         /* Find the SPI bus controller device with our selector string  */
                SpiDisplay = await SpiDevice.FromIdAsync(devicesInfo[0].Id, settings);  /* Create an SpiDevice with our bus controller and SPI settings */

            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                throw new Exception("SPI Initialization Failed", ex);
            }
        }


        private async Task InitDisplay()
        {
            /* Initialize the display */
            try
            {
                /* See the datasheet for more details on these commands: http://www.adafruit.com/datasheets/SSD1306.pdf             */
                await ResetDisplay();                   /* Perform a hardware reset on the display                                  */
                DisplaySendCommand(CMD_CHARGEPUMP_ON);  /* Turn on the internal charge pump to provide power to the screen          */
                DisplaySendCommand(CMD_MEMADDRMODE);    /* Set the addressing mode to "horizontal"                                  */
                DisplaySendCommand(CMD_SEGREMAP);       /* Flip the display horizontally, so it's easier to read on the breadboard  */
                DisplaySendCommand(CMD_COMSCANDIR);     /* Flip the display vertically, so it's easier to read on the breadboard    */
                DisplaySendCommand(CMD_DISPLAY_ON);     /* Turn the display on                                                      */
            }
            catch (Exception ex)
            {
                throw new Exception("Display Initialization Failed", ex);
            }
        }





        /* Send graphics data to the screen */
        private void DisplaySendData(byte[] Data)
        {
            /* When the Data/Command pin is high, SPI data is treated as graphics data  */
            DataCommandPin.Write(GpioPinValue.High);
            SpiDisplay.Write(Data);
        }

        /* Send commands to the screen */
        private void DisplaySendCommand(byte[] Command)
        {
            /* When the Data/Command pin is low, SPI data is treated as commands for the display controller */
            DataCommandPin.Write(GpioPinValue.Low);
            SpiDisplay.Write(Command);
        }

        /* Perform a hardware reset of the display */
        private async Task ResetDisplay()
        {
            ResetPin.Write(GpioPinValue.Low);   /* Put display into reset                       */
            await Task.Delay(1);                /* Wait at least 3uS (We wait 1mS since that is the minimum delay we can specify for Task.Delay() */
            ResetPin.Write(GpioPinValue.High);  /* Bring display out of reset                   */
            await Task.Delay(100);              /* Wait at least 100mS before sending commands  */
        }


        private async void InitSpiDisplay()
        {
            try
            {
                InitGpio3();             /* Initialize the GPIO controller and GPIO pins */
                await InitSpi();        /* Initialize the SPI controller                */
            //    await InitDisplay();    /* Initialize the display                       *///
            }
            /* If initialization fails, display the exception and stop running */
            catch (Exception ex)
            {
                greetingOutput.Text = "Exception: " + ex.Message;
                if (ex.InnerException != null)
                {
                    greetingOutput.Text += "\nInner Exception: " + ex.InnerException.Message;
                }
                return;
            }

            /* Register a handler so we update the SPI display anytime the user edits a textbox */
//              Display_TextBoxLine0.TextChanged += Display_TextBox_TextChanged;
 //           Display_TextBoxLine1.TextChanged += Display_TextBox_TextChanged;
 //           Display_TextBoxLine2.TextChanged += Display_TextBox_TextChanged;
 //           Display_TextBoxLine3.TextChanged += Display_TextBox_TextChanged;

            /* Manually update the display once after initialization*/
            DisplayTextBoxContents();

            greetingOutput.Text = "Status: Initialized";


        }





        /* Writes the Display Buffer out to the physical screen for display */
        private void DisplayUpdate()
        {
            int Index = 0;
            /* We convert our 2-dimensional array into a serialized string of bytes that will be sent out to the display */
            for (int PageY = 0; PageY < SCREEN_HEIGHT_PAGES; PageY++)
            {
                for (int PixelX = 0; PixelX < SCREEN_WIDTH_PX; PixelX++)
                {
                    SerializedDisplayBuffer[Index] = DisplayBuffer[PixelX, PageY];
                    Index++;
                }
            }

            /* Write the data out to the screen */
            DisplaySendCommand(CMD_RESETCOLADDR);         /* Reset the column address pointer back to 0 */
            DisplaySendCommand(CMD_RESETPAGEADDR);        /* Reset the page address pointer back to 0   */
            DisplaySendData(SerializedDisplayBuffer);     /* Send the data over SPI                     */
        }

        /* 
         * NAME:        WriteLineDisplayBuf
         * DESCRIPTION: Writes a string to the display screen buffer (DisplayUpdate() needs to be called subsequently to output the buffer to the screen)
         * INPUTS:
         *
         * Line:      The string we want to render. In this sample, special characters like tabs and newlines are not supported.
         * Col:       The horizontal column we want to start drawing at. This is equivalent to the 'X' axis pixel position.
         * Row:       The vertical row we want to write to. The screen is divided up into 4 rows of 16 pixels each, so valid values for Row are 0,1,2,3.
         *
         * RETURN VALUE:
         * None. We simply return when we encounter characters that are out-of-bounds or aren't available in the font.
         */
        private void WriteLineDisplayBuf(String Line, UInt32 Col, UInt32 Row)
        {
            UInt32 CharWidth = 0;
            foreach (Char Character in Line)
            {
                CharWidth = WriteCharDisplayBuf(Character, Col, Row);
                Col += CharWidth;   /* Increment the column so we can track where to write the next character   */
                if (CharWidth == 0) /* Quit if we encounter a character that couldn't be printed                */
                {
                    return;
                }
            }
        }

        /* 
         * NAME:        WriteCharDisplayBuf
         * DESCRIPTION: Writes one character to the display screen buffer (DisplayUpdate() needs to be called subsequently to output the buffer to the screen)
         * INPUTS:
         *
         * Character: The character we want to draw. In this sample, special characters like tabs and newlines are not supported.
         * Col:       The horizontal column we want to start drawing at. This is equivalent to the 'X' axis pixel position.
         * Row:       The vertical row we want to write to. The screen is divided up into 4 rows of 16 pixels each, so valid values for Row are 0,1,2,3.
         *
         * RETURN VALUE:
         * We return the number of horizontal pixels used. This value is 0 if Row/Col are out-of-bounds, or if the character isn't available in the font.
         */
        private UInt32 WriteCharDisplayBuf(Char Chr, UInt32 Col, UInt32 Row)
        {
            /* Check that we were able to find the font corresponding to our character */
            FontCharacterDescriptor CharDescriptor = DisplayFontTable.GetCharacterDescriptor(Chr);
            if (CharDescriptor == null)
            {
                return 0;
            }

            /* Make sure we're drawing within the boundaries of the screen buffer */
            UInt32 MaxRowValue = (SCREEN_HEIGHT_PAGES / DisplayFontTable.FontHeightBytes) - 1;
            UInt32 MaxColValue = SCREEN_WIDTH_PX;
            if (Row > MaxRowValue)
            {
                return 0;
            }
            if ((Col + CharDescriptor.CharacterWidthPx + DisplayFontTable.FontCharSpacing) > MaxColValue)
            {
                return 0;
            }

            UInt32 CharDataIndex = 0;
            UInt32 StartPage = Row * 2;
            UInt32 EndPage = StartPage + CharDescriptor.CharacterHeightBytes;
            UInt32 StartCol = Col;
            UInt32 EndCol = StartCol + CharDescriptor.CharacterWidthPx;
            UInt32 CurrentPage = 0;
            UInt32 CurrentCol = 0;

            /* Copy the character image into the display buffer */
            for (CurrentPage = StartPage; CurrentPage < EndPage; CurrentPage++)
            {
                for (CurrentCol = StartCol; CurrentCol < EndCol; CurrentCol++)
                {
                    DisplayBuffer[CurrentCol, CurrentPage] = CharDescriptor.CharacterData[CharDataIndex];
                    CharDataIndex++;
                }
            }

            /* Pad blank spaces to the right of the character so there exists space between adjacent characters */
            for (CurrentPage = StartPage; CurrentPage < EndPage; CurrentPage++)
            {
                for (; CurrentCol < EndCol + DisplayFontTable.FontCharSpacing; CurrentCol++)
                {
                    DisplayBuffer[CurrentCol, CurrentPage] = 0x00;
                }
            }

            /* Return the number of horizontal pixels used by the character */
            return CurrentCol - StartCol;
        }

        /* Sets all pixels in the screen buffer to 0 */
        private void ClearDisplayBuf()
        {
            Array.Clear(DisplayBuffer, 0, DisplayBuffer.Length);
        }

        /* Update the SPI display to mirror the textbox contents */
        private void DisplayTextBoxContents()
        {
            try
            {
                ClearDisplayBuf();  /* Blank the display buffer             */
                WriteLineDisplayBuf(Display_TextBoxLine0.Text, 0, 0);
 //               WriteLineDisplayBuf(Display_TextBoxLine1.Text, 0, 1);
 //               WriteLineDisplayBuf(Display_TextBoxLine2.Text, 0, 2);
 //               WriteLineDisplayBuf(Display_TextBoxLine3.Text, 0, 3);
                DisplayUpdate();    /* Write our changes out to the display */
            }
            /* Show an error if we can't update the display */
            catch (Exception ex)
            {
                greetingOutput.Text = "Status: Failed to update display";
                greetingOutput.Text = "\nException: " + ex.Message;
            }
        }

        /* Updates the display when the textbox contents change */
        private void Display_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DisplayTextBoxContents();
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            /* Cleanup */
            SpiDisplay.Dispose();
            ResetPin.Dispose();
            DataCommandPin.Dispose();
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

            greetingOutput.Text += distanceToObstacle.ToString()+"\n";


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

                ttt = "5555555555555555555555555555555555555555555";
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




    }




}





