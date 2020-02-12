import com.google.protobuf.ByteString;
import io.grpc.stub.*;
import mytorrent.grpc.*;

import java.util.logging.Level;
import java.util.logging.Logger;

public class TorrentService extends TorrentServiceGrpc.TorrentServiceImplBase
{
    private static final Logger logger = Logger.getLogger(TorrentService.class.getName());

    public TorrentService()
    {

    }

    /**
     * Returns for a specific file the locations of all fragments in the torrent network
     * and their order to join the back together.
     *
     * @param request Request that specifies the file for whom the locations of all corresponding fragments in the
     *                torrent network and their order to join the back together should be returned.
     * @param responseObserver Observer to return the response for the request.
     */
    @Override
    public void getFileDistribution(FileDistributionRequest request, StreamObserver<FileDistributionResponse> responseObserver)
    {
        logger.log(Level.INFO, "Request started in 'getFileDistribution' Route");

        FileDistributionResponse.Builder responseBuilder = FileDistributionResponse.newBuilder();
        responseBuilder.addFragmentOrder("First Fragment");
        responseBuilder.addFragmentOrder("Second Fragment");

        FragmentHolderList.Builder fragmentHolderListBuilder = FragmentHolderList.newBuilder();
        fragmentHolderListBuilder.addEndPoints("Endpoint");

        responseBuilder.putFragmentDistribution("FragmentHash", fragmentHolderListBuilder.build());

        responseObserver.onNext(responseBuilder.build());
        responseObserver.onCompleted();

        logger.log(Level.INFO, "Request completed in 'getFileDistribution' Route");
    }

    /**
     * Returns the byte data of fragments with specific hash values.
     *
     * @param responseObserver
     * @return
     */
    @Override
    public StreamObserver<FragmentDownloadRequest> downloadFileFragment(final StreamObserver<FragmentDownloadResponse> responseObserver)
    {
        logger.log(Level.INFO, "Request started in 'downloadFileFragment' Route");

        return new StreamObserver<FragmentDownloadRequest>()
        {
            @Override
            public void onNext(FragmentDownloadRequest fragmentDownloadRequest)
            {
                FragmentDownloadResponse.Builder responseBuilder = FragmentDownloadResponse.newBuilder();
                responseBuilder.setData(ByteString.copyFrom("hello World!".getBytes()));

                responseObserver.onNext(responseBuilder.build());
            }

            @Override
            public void onError(Throwable throwable)
            {
                logger.log(Level.WARNING, "Encountered error in 'downloadFileFragment' Route", throwable);
            }

            @Override
            public void onCompleted()
            {
                responseObserver.onCompleted();
                logger.log(Level.INFO, " Request completed in 'downloadFileFragment' Route");
            }
        };
    }
}
