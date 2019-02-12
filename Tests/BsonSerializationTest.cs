using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using NUnit.Framework;
using WebGame.Domain;

namespace Tests
{
    [TestFixture]
    public class BsonSerializationTest
    {
        [Test]
        public void CanSerializeUser()
        {
            var userEntity = new UserEntity("someUserId", "someUserName") { CurrentGameId = "someGameId" };
            AssertCorrectSerialization(userEntity);
        }

        [Test]
        public void CanSerializeGame()
        {
            var players = new List<Player>
            {
                new Player("userId1", "name1") { Decision = PlayerDecision.Paper, Score = 42 },
                new Player("userId2", "name2") { Decision = PlayerDecision.Rock, Score = 40 }
            };
            var entity = new GameEntity("someGameId")
            {
                CurrentTurnIndex = 2,
                Status = GameStatus.Playing,
                Players = players
            };
            AssertCorrectSerialization(entity);
        }

        [Test]
        public void CanSerializeNotStartedGame()
        {
            var entity = new GameEntity("someGameId");
            AssertCorrectSerialization(entity);
        }

        [Test]
        public void CanSerializeNotStartedGameWithPlayers()
        {
            var players = new List<Player> { new Player("userId", "name") };
            var entity = new GameEntity("someGameId") { CurrentTurnIndex = 2, Status = GameStatus.Playing, Players = players };
            AssertCorrectSerialization(entity);
        }

        private static void AssertCorrectSerialization<TEntity>(TEntity entity)
        {
            var memoryStream = new MemoryStream();
            BsonSerializer.Serialize(new BsonBinaryWriter(memoryStream), entity);
            var bytes = memoryStream.ToArray();
            var deserializedEntity = BsonSerializer.Deserialize<TEntity>(new MemoryStream(bytes));
            Console.WriteLine(deserializedEntity);
            deserializedEntity.Should().BeEquivalentTo(entity);
        }
    }
}