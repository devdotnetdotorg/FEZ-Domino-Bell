using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace FEZ_Domino_LCD.Drivers
{
    /// <summary> 
    /// FEZ Driver for the DFRobot I2C/TWI LCD1602 Module 
    /// http://www.dfrobot.com/index.php?route=product/product&path=53&product_id=135 
    ///  
    /// This display uses a JHD 162A LCD module with a DFRobot I2C Module 
    /// The I2C module uses a PCA8574 I/O Expander at Address 0x27 
    /// http://www.nxp.com/documents/data_sheet/PCA8574_PCA8574A.pdf 
    ///  
    /// Code is adapted from the arduino code:  
    /// http://www.dfrobot.com/image/data/DFR0063/Arduino_library.zip 
    ///  
    /// The module should be connected to the I2C port on the FEZ - sda (Data2) and scl (Data3) 
    ///  
    /// Refer to documentation on the Hitachi HD44780 for more detailed operational information 
    /// Eg: http://lcd-linux.sourceforge.net/pdfdocs/lcd1.pdf 
    /// </summary> 
    public class I2C_LCD
    {
        // The following are the first 4 bits of each byte. 

        const byte RS = 0x01;  // Register select bit. 0=command 1=data 
        const byte RW = 0x02;  // Read/Write bit.  We usually want to write (0). 
        const byte EN = 0x04;  // Enable bit. Data is set on the falling edge - see hitachi doco 
        // flags for backlight control 
        const byte LCD_BACKLIGHT = 0x08;
        const byte LCD_NOBACKLIGHT = 0x00;

        // The following are the high 4 bits - compounded with the flags below 
        // Note that everything must be done in 4bit mode, so set 4bit mode first. 

        // commands 
        const byte LCD_CLEARDISPLAY = 0x01;
        const byte LCD_RETURNHOME = 0x02;
        const byte LCD_ENTRYMODESET = 0x04;
        const byte LCD_DISPLAYCONTROL = 0x08;
        const byte LCD_CURSORSHIFT = 0x10;
        const byte LCD_FUNCTIONSET = 0x20;
        const byte LCD_SETCGRAMADDR = 0x40;
        const byte LCD_SETDDRAMADDR = 0x80;

        // Flags to be used with the above commands 

        // flags for display entry mode (0x04) 
        const byte LCD_ENTRYRIGHT = 0x00;
        const byte LCD_ENTRYLEFT = 0x02;
        const byte LCD_ENTRYSHIFTINCREMENT = 0x01;
        const byte LCD_ENTRYSHIFTDECREMENT = 0x00;

        // flags for display on/off control (0x08) 
        const byte LCD_DISPLAYON = 0x04;
        const byte LCD_DISPLAYOFF = 0x00;
        const byte LCD_CURSORON = 0x02;
        const byte LCD_CURSOROFF = 0x00;
        const byte LCD_BLINKON = 0x01;
        const byte LCD_BLINKOFF = 0x00;

        // flags for display/cursor shift (0x10) 
        const byte LCD_DISPLAYMOVE = 0x08;
        const byte LCD_CURSORMOVE = 0x00;
        const byte LCD_MOVERIGHT = 0x04;
        const byte LCD_MOVELEFT = 0x00;

        // flags for function set (0x20) 
        const byte LCD_8BITMODE = 0x10;
        const byte LCD_4BITMODE = 0x00;
        const byte LCD_2LINE = 0x08;
        const byte LCD_1LINE = 0x00;
        const byte LCD_5x10DOTS = 0x04;
        const byte LCD_5x8DOTS = 0x00;

        private I2CDevice MyI2C;
        private byte backLight = LCD_BACKLIGHT;

        private byte[] cmd_arr1;
        private byte[] cmd_arr2;
        private byte[] cmd_arr3;

        /// <summary> 
        /// Writes a byte in 4bit mode. 
        /// </summary> 
        /// <param name="byteOut">The byte to write</param> 
        /// <param name="mode">Additional Parameters - eg RS for data mode</param> 
        public void write4bit(byte byteOut, byte mode = 0)
        {
            write((byte)(byteOut & 0xF0), mode);
            write((byte)((byteOut << 4) & 0xF0), mode);
        }


        /// <summary> 
        /// Writes a byte to the I2C LCD. 
        /// </summary> 
        /// <param name="byteOut">The byte to write</param> 
        /// <param name="mode">Additional Parameters - eg RS for data mode</param> 
        public void write(byte byteOut, byte mode = 0)
        {
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[3];
            // Write the byte 
            cmd_arr1 = new byte[] { (byte)(byteOut | backLight | mode) };
            xActions[0] = I2CDevice.CreateWriteTransaction(cmd_arr1);
            // Set the En bit high 
            cmd_arr2 = new byte[] { (byte)(byteOut | backLight | mode | EN) };
            xActions[1] = I2CDevice.CreateWriteTransaction(cmd_arr2);
            // Set the En bit low 
            cmd_arr3 = new byte[] { (byte)(byteOut | backLight | mode | ~EN) };
            xActions[2] = I2CDevice.CreateWriteTransaction(cmd_arr3);

            // Write the commands (it seems to work without any delays added between calls). 
            MyI2C.Execute(xActions, 1000);
        }

        /// <summary> 
        ///  Prints text at current location 
        /// </summary> 
        /// <param name="text"></param> 
        public void print(string text)
        {
            for (int i = 0; i < text.Length; i++)
                write4bit((byte)(text[i]), RS);
        }

        /// <summary> 
        /// Clear screen and return to home 
        /// </summary> 
        public void clear()
        {
            write4bit(LCD_CLEARDISPLAY);
            write4bit(LCD_RETURNHOME);
        }

        /// <summary> 
        /// Sets the cursor position.  Zero based column and row. 
        /// </summary> 
        /// <param name="col"></param> 
        /// <param name="row"></param> 
        public void setCursor(byte col, byte row)
        {
            byte[] row_offsets = { 0x00, 0x40, 0x14, 0x54 };
            write4bit((byte)(LCD_SETDDRAMADDR | (col + row_offsets[row])));
        }

        /// <summary> 
        /// Turn the backlight off. 
        /// </summary> 
        public void backLightOff()
        {
            backLight = LCD_NOBACKLIGHT;
            write(0);
        }

        /// <summary> 
        /// Turn the backlight on. 
        /// </summary> 
        public void backLightOn()
        {
            backLight = LCD_BACKLIGHT;
            write(0);
        }

        public I2C_LCD(ushort address = 0x27, int clockRateKhz = 400)
        {
            I2CDevice.Configuration con = new I2CDevice.Configuration(address, clockRateKhz);
            MyI2C = new I2CDevice(con);

            // Set 4 Bit mode - copied from arduino code 
            write(LCD_FUNCTIONSET | LCD_8BITMODE);
            write(LCD_FUNCTIONSET | LCD_8BITMODE);
            write(LCD_FUNCTIONSET | LCD_8BITMODE);
            write(LCD_FUNCTIONSET | LCD_4BITMODE);

            // COMMAND | FLAG1 | FLAG2 | ... 
            write4bit(LCD_FUNCTIONSET | LCD_4BITMODE | LCD_2LINE | LCD_5x8DOTS);
            write4bit(LCD_DISPLAYCONTROL | LCD_DISPLAYON);

            // Screen may not be cleared after a reset 
            clear();
        }

    }
} 
