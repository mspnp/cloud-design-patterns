$connection = "localhost:19000"

Connect-ServiceFabricCluster $connection

Restart-ServiceFabricNode -NodeName _Node_0 -CommandCompletionMode DoNotVerify
Restart-ServiceFabricNode -NodeName _Node_1 -CommandCompletionMode DoNotVerify
Restart-ServiceFabricNode -NodeName _Node_2 -CommandCompletionMode DoNotVerify
Restart-ServiceFabricNode -NodeName _Node_3 -CommandCompletionMode DoNotVerify
Restart-ServiceFabricNode -NodeName _Node_4 -CommandCompletionMode DoNotVerify