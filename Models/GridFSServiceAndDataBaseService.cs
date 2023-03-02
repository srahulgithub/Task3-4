using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace Assignment4.Models
{
    public class GridFSServiceAndDataBaseService
    {
        private readonly IMongoCollection<BsonDocument> table;
        private readonly GridFSBucket bucket;
        public GridFSServiceAndDataBaseService(IOptions<UsersDatabaseSetting> options)
        {
            var client = new MongoClient(options.Value.ConnectionString);
            var database = client.GetDatabase(options.Value.DatabaseName);
            table = database.GetCollection<BsonDocument>(options.Value.UsersCollectionName);
            var cond = new GridFSBucketOptions
            {
                BucketName = "Image",
                ChunkSizeBytes = 255 
            };
            bucket = new(database, cond);
        }
        public async Task Create(BsonDocument userUploadFile)
        {
            await table.InsertOneAsync(userUploadFile);
        }
        public async Task<List<BsonDocument>> Get(string ip)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("IP", ip);
            return await table.Find(filter).ToListAsync();
        }

        public async Task UpdateDescriptionAsync(UpdateString st)
        {
            var filter = Builders<BsonDocument>.Filter.Eq("file_id", st.id);
            var update = Builders<BsonDocument>.Update.Set("Description", st.description);
            await table.UpdateOneAsync(filter, update);
        }
        public async Task<string> UploadAsync(IFormFile file)
        {
            var type = file.ContentType.ToString();
            var fileName = file.FileName;

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument { { "FileName", fileName }, { "Type", type } }
            };

            using var stream = await bucket.OpenUploadStreamAsync(fileName, options);
            var id = stream.Id; 
            file.CopyTo(stream); 
            await stream.CloseAsync();
            return id.ToString();
        }
        public async Task<byte[]> GetFileByIdAsync(ObjectId Id)
        {
            return await bucket.DownloadAsBytesAsync(Id);

        }
        public async Task<string> UpdateImageAsync(UpdateImagecs upfile)
        {
            
            IFormFile file = upfile.image;
            var type = file.ContentType.ToString();
            var fileName = file.FileName;

            var options = new GridFSUploadOptions
            {
                Metadata = new BsonDocument { { "FileName", fileName }, { "Type", type } }
            };

            using var stream = await bucket.OpenUploadStreamAsync(fileName, options); 
            var id = stream.Id; 
            file.CopyTo(stream); 
            await stream.CloseAsync();

            var ipNew = new ObjectId(upfile.id);
            await bucket.DeleteAsync(ipNew);
            var filter = Builders<BsonDocument>.Filter.Eq("file_id", upfile.id);
            var update = Builders<BsonDocument>.Update.Set("file_id", id.ToString());
            await table.UpdateOneAsync(filter, update);
            return id.ToString();
        }

    }
}