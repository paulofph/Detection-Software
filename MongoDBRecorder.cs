    using MongoDB.Bson;
    using MongoDB.Driver;
    using MongoDB.Driver.Linq;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using MongoDB.Bson;
    using MongoDB.Bson.IO;
    using MongoDB.Bson.Serialization;
    using MongoDB.Driver;
using System;
using System.IO;
//using FluentAssertions;

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using MongoDB.Driver.Builders;
    class MongoDBRecorder
    {
        private static IMongoClient _client;
        private static IMongoDatabase _database;
        private static IMongoCollection<BsonDocument> _collection1;
        private static IMongoCollection<BsonDocument> _collection2;

        /// <summary> The body index (0-5) associated with the current gesture detector </summary>
        private int _bodyIndex = 0;

        /// <summary> Current confidence value reported by the discrete gesture </summary>
        private float _confidence = 0.0f;

        /// <summary> True, if the discrete gesture is currently being detected </summary>
        private bool _detected = false;

        /// <summary> True, if the body is currently being tracked </summary>
        private bool _isTracked = false;

        /// <summary> testList </summary>
        private List<int> teste = new List<int>();

        int i = 0;
      

        /// <summary>
        /// Initializes a new instance of the GestureResultView class and sets initial property values
        /// </summary>
        /// <param name="bodyIndex">Body Index associated with the current gesture detector</param>
        /// <param name="isTracked">True, if the body is currently tracked</param>
        /// <param name="detected">True, if the gesture is currently detected for the associated body</param>
        /// <param name="confidence">Confidence value for detection of the 'Seated' gesture</param>
        public MongoDBRecorder(int bodyIndex, bool isTracked, bool detected, float confidence)
        {
            var collName1 = "GestureBuildEvents";
            var collName2 = "RecentActivity";

            MongoClient _client = new MongoClient();
            _database = _client.GetDatabase("AHA");

            CreateCappedCollectionAsyncVGB(collName1);
            CreateCappedCollectionAsyncRecent(collName2);
            _collection1 = _database.GetCollection<BsonDocument>(collName1);
            _collection2 = _database.GetCollection<BsonDocument>(collName2);

            var document = new BsonDocument
                        {
                            { "detected", false },
                            { "confidence", 0 },
                            { "gesture", "Null" },
                            { "timeStamp", DateTime.UtcNow },
                        };
            _collection1.InsertOneAsync(document).Wait();
            _collection2.InsertOneAsync(document).Wait();
        }

        /// <summary>
        /// Updates the values associated with the discrete gesture detection result
        /// </summary>
        /// <param name="isBodyTrackingIdValid">True, if the body associated with the GestureResultView object is still being tracked</param>
        /// <param name="isGestureDetected">True, if the discrete gesture is currently detected for the associated body</param>
        /// <param name="detectionConfidence">Confidence value for detection of the discrete gesture</param>
        public void UpdateMongoDB (bool isBodyTrackingIdValid, bool isGestureDetected, float detectionConfidence, string gestureName)
        {
            this._isTracked = isBodyTrackingIdValid;
            this._confidence = detectionConfidence;

            if (!this._isTracked)
            {
                this._detected = false;
            }
            else
            {
                this._detected = isGestureDetected;

                if (this._detected)
                {
                    // "Pick" da última entrada na colecção GestureBuildEvents
                    var VGBEvents = _database.GetCollection<BsonDocument>("GestureBuildEvents");
                    var recentEvents = _database.GetCollection<BsonDocument>("RecentActivity");
                    var filterBuilder = Builders<BsonDocument>.Filter;
                    var filter = new BsonDocument();
                    var Item = VGBEvents.Find(filter)
                                         .Sort(Builders<BsonDocument>.Sort.Descending("timeStamp"))
                                         .Limit(1).ToList();
                    var timeStamp = Item[0].Values.ToArray()[4].ToUniversalTime(); // timeStamp da última entrada da colecção VGBEvents

                    var actualTime = DateTime.UtcNow;

                    //Diferença temporal entre o gesto atual e o mais recente da colecção GestureBuildEvents
                    var timeDifference = actualTime - timeStamp;

                    //Verificação da diferença temporal entre a ultima publicação e o gesto actual.
                    if (timeDifference.TotalSeconds >= 0.25) //1 0.25
                    {
                        // Busca do ultimo documento da colecção RecentEvents
                        Item = recentEvents.Find(filter)
                                           .Sort(Builders<BsonDocument>.Sort.Descending("timeStamp"))
                                           .Limit(1).ToList();

                        timeStamp = Item[0].Values.ToArray()[4].ToUniversalTime();

                        //Diferença temporal entre o gesto atual e o mais recente da colecção RecentActivity
                        timeDifference = actualTime - timeStamp;

                        if (timeDifference.TotalSeconds >= 2) //2
                        {
                            // "Drop" da colecção RecentActivity
                            _database.DropCollectionAsync("RecentActivity");


                            // iniciação da colecção
                            CreateCappedCollectionAsyncRecent("RecentActivity");
                            recentEvents = _database.GetCollection<BsonDocument>("RecentActivity");

                            // inserção do documento na colecção "recentActivity"
                            var document = new BsonDocument
                            {
                                { "detected", this._detected },
                                { "confidence", this._confidence },
                                { "gesture", gestureName },
                                { "timeStamp", DateTime.UtcNow },
                            };
                            recentEvents.InsertOneAsync(document).Wait();

                        }
                        else //2
                        {
                            // inserção do documento na colecção "recentActivity"
                            var document = new BsonDocument
                            {
                                { "detected", this._detected },
                                { "confidence", this._confidence },
                                { "gesture", gestureName },
                                { "timeStamp", DateTime.UtcNow },
                            };
                            recentEvents.InsertOneAsync(document).Wait();
                        }


                        // Intervalo temporal entre os documentos na colecção RecentActivity
                        filterBuilder = Builders<BsonDocument>.Filter;
                        filter = new BsonDocument();
                        var Item1 = recentEvents.Find(filter)
                                             .Sort(Builders<BsonDocument>.Sort.Descending("timeStamp"))
                                             .Limit(1).ToList();

                        var Item2 = recentEvents.Find(filter) //    /////2
                                             .Sort(Builders<BsonDocument>.Sort.Ascending("timeStamp"))
                                             .Limit(1).ToList();

                        var timeStampItem1 = Item1[0].Values.ToArray()[4].ToUniversalTime();
                        var timeStampItem2 = Item2[0].Values.ToArray()[4].ToUniversalTime();
                        timeDifference = timeStampItem1 - timeStampItem2;

                        if (timeDifference.TotalSeconds >= 0)//3    0 = 2
                        {
                            // criação de dicionário
                            Dictionary<string, double> dictionary = new Dictionary<string, double>();

                            dictionary.Add("Bye_bye", 0.0f);
                            dictionary.Add("Calm_down", 0.0f);
                            dictionary.Add("Come_here", 0.0f);
                            dictionary.Add("Confident_1", 0.0f);
                            dictionary.Add("Confident_2", 0.0f);
                            dictionary.Add("Dont_understand", 0.0f);
                            dictionary.Add("Finish_session", 0.0f);
                            dictionary.Add("Go_out", 0.0f);
                            dictionary.Add("Have_a_question", 0.0f);
                            dictionary.Add("Next_exercise", 0.0f);
                            dictionary.Add("Ok_1", 0.0f);
                            dictionary.Add("Ok_2", 0.0f);
                            dictionary.Add("Pay_attention", 0.0f);
                            dictionary.Add("Phone_call", 0.0f);
                            dictionary.Add("Rotate", 0.0f);
                            dictionary.Add("Stop", 0.0f);



                            // pick de todos os documentos nas na colecção
                            filterBuilder = Builders<BsonDocument>.Filter;
                            filter = new BsonDocument();
                            var finalList = recentEvents.Find(filter).ToList();

                            //loop em todos os documentos da colecção recentActivity,
                            //adição dos valores da confiança da colecção ao gesto correspondente no dicionário
                            for (int i = 0; i < finalList.Count; i++)
                            {
                                var collectionGestureName = finalList[i].Values.ToArray()[3].ToString();
                                var collectionGesturConfidence = finalList[i].Values.ToArray()[2].ToDouble();
                                if (collectionGestureName != "Null")
                                {
                                    dictionary[collectionGestureName] += collectionGesturConfidence;
                                }
                            }

                            // Pick da key do dicionário com maior confiança associada.

                            var document = new BsonDocument
                            {
                                { "detected", this._detected },
                                { "confidence", dictionary.Values.Max()},
                                { "gesture", dictionary.Keys.Max()},
                                { "timeStamp", DateTime.UtcNow },
                            };
                            VGBEvents.InsertOneAsync(document).Wait();
                        }
                        else //3
                        {

                        }
                    }//1
                }
                else
                {
                }
            }
        }

        private static void CreateCappedCollectionAsyncVGB(string collname)
		{
			_database.CreateCollectionAsync(collname, new CreateCollectionOptions
			{
				Capped = true,
				MaxSize = 50000,
				MaxDocuments = 1000,
				});
		}

        private static void CreateCappedCollectionAsyncRecent(string collname)
		{
			_database.CreateCollectionAsync(collname, new CreateCollectionOptions
			{
				Capped = true,
				MaxSize = 50000,
				MaxDocuments = 8,
				});
		}

    }
}
