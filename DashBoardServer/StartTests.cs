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
        public void StopEvent(object RESPONSE)
        {
            connectDemon.StopTestsInDemon(RESPONSE);
            return;
        }
    }
}