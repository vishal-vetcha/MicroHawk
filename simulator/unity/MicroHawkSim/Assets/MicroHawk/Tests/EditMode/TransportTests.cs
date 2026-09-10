using System;
using System.IO;
using MicroHawk.Transport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
namespace MicroHawk.Tests
{
    public sealed class TransportTests
    {
        private static JObject Valid()=>WireProtocol.Parse(File.ReadAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../contracts/fixtures/v1/valid_takeoff.json"))));
        [Test] public void SharedFixturesRejectUnknownFieldsTypesAndActions()
        {
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../contracts/fixtures/v1"));
            foreach(string path in Directory.GetFiles(root,"*.json"))
                if(Path.GetFileName(path).StartsWith("valid_"))Assert.DoesNotThrow(()=>WireProtocol.Decode(WireProtocol.Parse(File.ReadAllText(path))));
                else Assert.Throws<ArgumentException>(()=>WireProtocol.Decode(WireProtocol.Parse(File.ReadAllText(path))),path);
        }
        [Test] public void DuplicateIsIdempotentAndChangedPayloadRejected()
        {
            var p=new WireProtocol("session");var command=Valid();p.Admit(command,0,"authority",out _);p.Outcome("test1",10,"Completed","None");
            Assert.That(p.Admit(command,20,"authority",out var replay),Is.Null);Assert.That((string)replay["status"],Is.EqualTo("Completed"));
            command["parameters"]["altitude"]=5;Assert.Throws<ArgumentException>(()=>p.Admit(command,20,"authority",out _));
        }
        [TestCase("session_id","other")][TestCase("authority_id","other")]
        public void StaleSessionAndAuthorityReject(string key,string value){var c=Valid();c[key]=value;Assert.Throws<ArgumentException>(()=>new WireProtocol("session").Admit(c,0,"authority",out _));}
        [Test] public void ExpiryFutureAndSequenceReject()
        {
            Assert.Throws<ArgumentException>(()=>new WireProtocol("session").Admit(Valid(),100,"authority",out _));
            var c=Valid();c["issued_at_tick"]=10;Assert.Throws<ArgumentException>(()=>new WireProtocol("session").Admit(c,0,"authority",out _));
            var p=new WireProtocol("session");p.Admit(Valid(),0,"authority",out _);c=Valid();c["command_id"]="new";Assert.Throws<ArgumentException>(()=>p.Admit(c,0,"authority",out _));
        }
        [Test] public void ReconnectRestartsSequenceButRequiresFreshAuthority()
        {
            var p=new WireProtocol("session");p.Admit(Valid(),0,"authority",out _);p.BeginAuthority();
            var c=Valid();c["command_id"]="next";c["authority_id"]="new";
            Assert.That(p.Admit(c,0,"new",out _),Is.Not.Null);
            Assert.Throws<ArgumentException>(()=>p.Admit(Valid(),0,"new",out _));
        }
        [Test] public void DuplicateJsonKeysReject()=>Assert.Throws<Newtonsoft.Json.JsonReaderException>(()=>WireProtocol.Parse("{\"type\":1,\"type\":2}"));
    }
}
