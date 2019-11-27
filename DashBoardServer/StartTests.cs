namespace DashBoardServer
{
    public class StartTests
    {
        private ConnectToDemon connectDemon = new ConnectToDemon();

        public void Init(object RESPONSE)
        {
            connectDemon.StartTestsInDemon(RESPONSE);
            return;
        }
    }
}