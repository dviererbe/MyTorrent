namespace MyTorrent.DistributionServices.Events
{
    public enum ClientJoinDeniedCode
    {
        WrongFragmentSize = 1,
        WrongHashAlgorithm = 2,
        EndpointConflict = 4,
        Other = 128
    }
}
