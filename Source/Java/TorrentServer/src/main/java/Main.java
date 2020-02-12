import io.grpc.Server;
import io.grpc.ServerBuilder;

import java.io.IOException;
import java.util.concurrent.TimeUnit;
import java.util.logging.Logger;

public class Main
{
    public static final int PORT = 50051;
    private static final Logger logger = Logger.getLogger(Main.class.getName());

    public static void main(String[] args) throws IOException, InterruptedException
    {
        final Server server = ServerBuilder.forPort(PORT)
            .addService(new TorrentService())
            .build()
            .start();

        logger.info("Server started, listening on " + PORT);

        Runtime.getRuntime().addShutdownHook(new Thread()
        {
            @Override
            public void run() {
                // Use stderr here since the logger may have been reset by its JVM shutdown hook.
                System.err.println("*** shutting down gRPC server since JVM is shutting down");

                try
                {
                    if (server != null)
                    {
                        server.shutdown().awaitTermination(30, TimeUnit.SECONDS);
                    }
                }
                catch (InterruptedException e)
                {
                    e.printStackTrace(System.err);
                }

                System.err.println("*** server shut down");
            }
        });

        server.awaitTermination();
    }
}
