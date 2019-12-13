using System;
using Microsoft.SPOT;
using GHI_LowLevel = GHIElectronics.NETMF.Hardware.LowLevel;
using GHIElectronics.NETMF.IO;
using Microsoft.SPOT.IO;
using System.IO;
using System.Threading;

namespace FEZ_Domino_Zvonok
{

    //����� ������������
    public class AppSettings
    {
        public short SyncTimeDelayMin;//�������� ������� ������������� ������� NTP ������ 30 ��� 1 ��� 2 ���� 12 ����� 24 ����
        public DateTime[] CurTimeBell;//����� ������ ������� - 4 �������
        //
        public AppSettings()
        {
            CurTimeBell = new DateTime[8]; //����� ������ ������
        }
        public void ReadSettings()
        {
            GHI_LowLevel.Watchdog.ResetCounter();
            //����������� SD �����
            Debug.Print("SD is=" + PersistentStorage.DetectSDCard().ToString());
            PersistentStorage sdPS = new PersistentStorage("SD");
            if (PersistentStorage.DetectSDCard())
            {
                sdPS.MountFileSystem();
            }
            //
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                FileStream FileHandle = new FileStream(rootDirectory +
                                          @"\AppSettings.config", FileMode.Open);
                StreamReader sw = new StreamReader(FileHandle);
                GHI_LowLevel.Watchdog.ResetCounter();
                ///////////////
                string str;
                str = sw.ReadLine();
                short short_SyncTimeDelayMin;
                short_SyncTimeDelayMin = Convert.ToInt16(str);
                SyncTimeDelayMin = short_SyncTimeDelayMin;
                ///////////////
                //�����
                for (short i = 0; i <= 7; i++) CurTimeBell[i] = ReadTimefromString(sw.ReadLine());
                GHI_LowLevel.Watchdog.ResetCounter();
                //
                sw.Close();
                sw.Dispose();
                FileHandle.Close();
                FileHandle.Dispose();
                VolumeInfo.GetVolumes()[0].FlushAll();
                Thread.Sleep(500);
                sdPS.UnmountFileSystem();
            }
            GHI_LowLevel.Watchdog.ResetCounter();
            Thread.Sleep(500);
            sdPS.Dispose();
            sdPS = null;
            Debug.Print("Read file ok");
            GHI_LowLevel.Watchdog.ResetCounter();
        }
        private static DateTime ReadTimefromString(string value)
        {
            string[] str1 = value.Split(':');
            DateTime dt = new DateTime(2008, 3, 1, Convert.ToInt32(str1[0]), Convert.ToInt32(str1[1]), 0);
            return dt;
        }
        public void SaveSettings()
        {
            //����������� SD �����
            Debug.Print("SD is=" + PersistentStorage.DetectSDCard().ToString());
            PersistentStorage sdPS = new PersistentStorage("SD");
            if (PersistentStorage.DetectSDCard())
            {
                sdPS.MountFileSystem();
            }
            //
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                //��� ���
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                FileStream FileHandle = new FileStream(rootDirectory +
                                          @"\AppSettings.config", FileMode.Create);
                StreamWriter sw = new StreamWriter(FileHandle);
                sw.WriteLine(SyncTimeDelayMin);
                //�����
                for (short i = 0; i <= 7; i++) sw.WriteLine(CurTimeBell[i].ToString("HH:mm"));
                sw.Flush();
                //
                FileHandle.Close();
                VolumeInfo.GetVolumes()[0].FlushAll();
                Thread.Sleep(500);
                sdPS.UnmountFileSystem();
            }
            Thread.Sleep(500);
            sdPS.Dispose();
            sdPS = null;
            Debug.Print("WR file ok");
        }
        public void RecordDefaultSettings()
        {
            //����������� SD �����
            Debug.Print("SD is=" + PersistentStorage.DetectSDCard().ToString());
            PersistentStorage sdPS = new PersistentStorage("SD");
            if (PersistentStorage.DetectSDCard())
            {
                sdPS.MountFileSystem();
            }
            //
            if (VolumeInfo.GetVolumes()[0].IsFormatted)
            {
                //��� ���
                string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                FileStream FileHandle = new FileStream(rootDirectory +
                                          @"\AppSettings.config", FileMode.Create);
                StreamWriter sw = new StreamWriter(FileHandle);
                sw.WriteLine(3);
                //�����
                for (short i = 0; i <= 7; i++) sw.WriteLine("8:10");
                sw.Flush();
                //
                FileHandle.Close();
                VolumeInfo.GetVolumes()[0].FlushAll();
                Thread.Sleep(500);
                sdPS.UnmountFileSystem();
            }
            Thread.Sleep(500);
            sdPS.Dispose();
            sdPS = null;
            Debug.Print("WR file ok");
        }
        public void CopyTo(AppSettings appset)
        {
            SyncTimeDelayMin = appset.SyncTimeDelayMin;
            //�����
            for (short i = 0; i <= 7; i++)
                CurTimeBell[i] = appset.CurTimeBell[i];
        }
    }
}
