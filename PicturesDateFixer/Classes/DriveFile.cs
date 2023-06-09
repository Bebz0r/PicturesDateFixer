﻿namespace PicturesDateFixer;
public partial class MainPage
{
    public class DriveFile
    {
        public string Name { get; set; }
        public string Folder { get; set; }
        public string FullPath { get; set; }
        public string Extension { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public DateTime? DateTimeOriginal { get; set; }
        public string CreatedStr
        {
            get
            {
                return Created.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        public string ModifiedStr
        {
            get
            {
                return Modified.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }

        public string DateTimeOriginalStr
        {
            get
            {
                return (DateTimeOriginal == null ? null : DateTimeOriginal.Value.ToString("dd/MM/yyyy HH:mm:ss"));
            }
        }

        public string CreatedStr2
        {
            get
            {
                return Created.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public string ModifiedStr2
        {
            get
            {
                return Modified.ToString("dd/MM/yyyy HH:mm");
            }
        }

        public string DateTimeOriginalStr2
        {
            get
            {
                return (DateTimeOriginal == null ? "absent" : DateTimeOriginal.Value.ToString("dd/MM/yyyy HH:mm"));
            }
        }

    }
}

