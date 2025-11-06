# Implementation Plan

- [x] 1. Create missing test entity types
  - Create entity classes for multi-entity table tests
  - Create entity classes for transaction operation tests
  - Add all properties referenced in test code
  - _Requirements: 1.3, 3.1, 3.2, 3.3_

- [x] 1.1 Create MultiEntityOrderTestEntity
  - Write entity class with DynamoDbEntity and DynamoDbTable attributes
  - Add Id property with PartitionKey attribute
  - Add CustomerName, TotalAmount, and Item properties
  - _Requirements: 3.1, 3.2_

- [x] 1.2 Create MultiEntityOrderLineEntity
  - Write entity class with DynamoDbEntity and DynamoDbTable attributes
  - Add Id property with PartitionKey attribute
  - Add ProductName and Quantity properties
  - _Requirements: 3.1, 3.2_

- [x] 1.3 Create TransactionOrderEntity
  - Write entity class with DynamoDbEntity and DynamoDbTable attributes
  - Add Id property with PartitionKey attribute
  - Add CustomerName and TotalAmount properties
  - _Requirements: 3.1, 3.2_

- [x] 1.4 Create TransactionOrderLineEntity
  - Write entity class with DynamoDbEntity and DynamoDbTable attributes
  - Add Id property with PartitionKey attribute
  - Add ProductName and Quantity properties
  - _Requirements: 3.1, 3.2_

- [x] 1.5 Create TransactionPaymentTestEntity
  - Write entity class with DynamoDbEntity and DynamoDbTable attributes
  - Add Id property with PartitionKey attribute
  - Add Amount and PaymentMethod properties
  - _Requirements: 3.1, 3.2_

- [x] 2. Add ReturnValues method to request builders
  - Implement ReturnValues() on UpdateItemRequestBuilder
  - Implement ReturnValues() on PutItemRequestBuilder
  - Ensure methods support fluent chaining
  - _Requirements: 1.4, 4.1, 4.3, 4.4_

- [x] 2.1 Add ReturnValues to UpdateItemRequestBuilder
  - Write ReturnValues method that accepts ReturnValue parameter
  - Set _request.ReturnValues property
  - Return this for method chaining
  - _Requirements: 4.1, 4.3, 4.4_

- [x] 2.2 Add ReturnValues to PutItemRequestBuilder
  - Write ReturnValues method that accepts ReturnValue parameter
  - Set _request.ReturnValues property
  - Return this for method chaining
  - _Requirements: 4.1, 4.3, 4.4_

- [ ] 3. Add ToDynamoDbResponseAsync to transaction builders
  - Implement ToDynamoDbResponseAsync() on TransactGetItemsRequestBuilder
  - Implement ToDynamoDbResponseAsync() on TransactWriteItemsRequestBuilder
  - Ensure methods return proper response types
  - _Requirements: 1.4, 4.2, 4.3_

- [ ] 3.1 Add ToDynamoDbResponseAsync to TransactGetItemsRequestBuilder
  - Write async method that calls ToRequest() and executes via client
  - Return TransactGetItemsResponse
  - Support CancellationToken parameter
  - _Requirements: 4.2, 4.3_

- [ ] 3.2 Add ToDynamoDbResponseAsync to TransactWriteItemsRequestBuilder
  - Write async method that calls ToRequest() and executes via client
  - Return TransactWriteItemsResponse
  - Support CancellationToken parameter
  - _Requirements: 4.2, 4.3_

- [ ] 4. Verify source generator produces table accessors
  - Build the project to trigger source generation
  - Verify MultiEntityTestTable is generated with Orders and OrderLines accessors
  - Verify TransactionTestTable is generated with Orders, OrderLines, and Payments accessors
  - _Requirements: 1.1, 1.2, 2.1, 2.2, 2.3, 2.4_

- [ ] 4.1 Rebuild project and check generated code
  - Execute dotnet build on integration test project
  - Check obj/Generated folder for generated table classes
  - Verify accessor properties exist in generated code
  - _Requirements: 2.1, 2.2, 2.3_

- [ ] 4.2 Verify build completes without errors
  - Run full solution build
  - Confirm all 170 errors are resolved
  - Check for any new warnings or issues
  - _Requirements: 1.1_

- [ ] 5. Verify integration tests pass
  - Run MultiEntityTableTests to verify table generation
  - Run TransactionOperationTests to verify transaction operations
  - Run OperationContextIntegrationTests to verify API methods
  - _Requirements: 1.1, 5.1, 5.2_
