using Microsoft.VisualStudio.TestTools.UnitTesting;
using Devopsbot;

namespace Devopsbttest
{
    [TestClass]
    public class UnitTest1
    {

        [TestMethod]
        public void DoesBotDetectVSTSInString()
        {   //arrange
            ITextProcessor InputText = new VSTSTextProcessor();

            //act
            var result = InputText.DetectVSTS("VSTS");

            //assert
            Assert.IsTrue(result);

        }
        [TestMethod]
        public void DoesBotDetectVSTSInEmptyString()
        {   //arrange
            ITextProcessor InputText = new VSTSTextProcessor();

            //act
            var result = InputText.DetectVSTS("");

            //assert
            Assert.IsFalse(result);

        }

    }

}

