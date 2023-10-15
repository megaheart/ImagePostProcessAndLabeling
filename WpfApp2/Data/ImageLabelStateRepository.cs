using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp2.Models;

namespace WpfApp2.Data
{
    public class ImageLabelStateRepository
    {
        string dbPath;
        string collectionName = "imageLabelState";
        public static void ConfigureBsonMapper()
        {
            var mapper = BsonMapper.Global;
            mapper.Entity<ImageLabelState>()
                .Id(x => x.ImagePath, false)
                .Ignore(x => x.StateVersion);
        }

        public ImageLabelStateRepository(string workspacePath) 
        { 
            this.dbPath = Path.Combine(workspacePath, "data.db");
        }
        public List<ImageLabelState> GetAll()
        {
            List<ImageLabelState> values = null;
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);
                values = col.FindAll().ToList();
            }

            return values;
        }
        public ImageLabelState Get(string imagePath)
        {
            ImageLabelState state = null;
            // Open database (or create if doesn't exist)
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);
                state = col.FindById(imagePath);
            }
            return state;
        }
        public ImageLabelState? Insert(ImageLabelState imageLabelState)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);
                try
                {
                    col.Insert(imageLabelState);
                }
                catch(Exception _)
                {
                    return null;
                }
            }

            return imageLabelState;
        }

        public ImageLabelState? Update(ImageLabelState imageLabelState)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);

                try
                {
                    col.Update(imageLabelState);
                }
                catch(Exception _)
                {
                    return null;
                }
            }

            return imageLabelState;
        }

        public ImageLabelState InsertOrReplace(ImageLabelState imageLabelState)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);
                col.Upsert(imageLabelState);
            }

            return imageLabelState;
        }

        public bool Delete(string imagePath)
        {
            using (var db = new LiteDatabase(dbPath))
            {
                // Get customer collection
                var col = db.GetCollection<ImageLabelState>(collectionName);
                try
                {
                    return col.Delete(imagePath);
                }
                catch(Exception _)
                {
                    return false;
                }
            }
        }

    }
}
