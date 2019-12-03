namespace DashBoardServer
{
    public class StartTests
    {
        private ConnectToDemon connectDemon = new ConnectToDemon();

        public void Event(object RESPONSE)
        {
            connectDemon.StartTestsInDemon(RESPONSE);
            return;
        }
    }
}