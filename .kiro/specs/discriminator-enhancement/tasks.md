# Implementation Plan

- [x] 1. Enhance attribute classes with new discriminator properties
  - Add DiscriminatorProperty, DiscriminatorValue, and DiscriminatorPattern to DynamoDbTableAttribute
  - Add GSI-specific discriminator properties to GlobalSecondaryIndexAttribute
  - Mark EntityDiscriminator as obsolete with appropriate message
  - _Requirements: 1.1, 1.2, 2.5, 3.1, 4.2, 4.4_

- [x] 2. Create discriminator configuration models
  - [x] 2.1 Implement DiscriminatorConfig class with PropertyName, ExactValue, Pattern, and Strategy properties
    - Define DiscriminatorStrategy enum with None, ExactMatch, StartsWith, EndsWith, Contains, and Custom values
    - Add validation logic to ensure configuration is valid
    - _Requirements: 1.1, 2.1, 2.2, 2.3, 2.4, 7.1_
  
  - [x] 2.2 Create DiscriminatorAnalyzer for pattern analysis
    - Implement CreateDiscriminatorConfig method to parse attribute parameters
    - Implement CreateLegacyDiscriminatorConfig for backward compatibility
    - Implement pattern analysis logic to determine optimal matching strategy
    - Add ExtractMatchingValue method to extract comparison value from patterns
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 4.1, 7.1, 7.2_

- [x] 3. Update entity and projection models
  - [x] 3.1 Add Discriminator property to EntityModel
    - Keep EntityDiscriminator for backward compatibility
    - Add HasDiscriminator helper property
    - _Requirements: 1.3, 4.1, 4.3_
  
  - [x] 3.2 Add GsiDiscriminator property to IndexModel
    - Add HasGsiDiscriminator helper property
    - _Requirements: 3.1, 3.2_
  
  - [x] 3.3 Update ProjectionModel with discriminator configurations
    - Add Discriminator and GsiDiscriminator properties
    - Implement GetEffectiveDiscriminator method to handle GSI override logic
    - Keep legacy DiscriminatorValue for backward compatibility
    - _Requirements: 3.2, 3.3, 4.1_

- [x] 4. Update EntityAnalyzer to parse new discriminator attributes
  - [x] 4.1 Parse DiscriminatorProperty, DiscriminatorValue, and DiscriminatorPattern from DynamoDbTableAttribute
    - Extract all discriminator-related named arguments
    - Create DiscriminatorConfig using DiscriminatorAnalyzer
    - Handle legacy EntityDiscriminator property
    - _Requirements: 1.1, 1.2, 2.5, 4.1, 4.2_
  
  - [x] 4.2 Parse GSI-specific discriminator properties from GlobalSecondaryIndexAttribute
    - Extract GSI discriminator arguments for each index
    - Create GSI-specific DiscriminatorConfig instances
    - Associate GSI discriminators with correct IndexModel
    - _Requirements: 3.1, 3.2_

- [x] 5. Update ProjectionModelAnalyzer to propagate discriminator configuration
  - Copy entity-level Discriminator to ProjectionModel
  - Copy GSI-specific discriminator when projection targets a GSI
  - Maintain backward compatibility with legacy DiscriminatorValue
  - _Requirements: 3.3, 4.1, 4.3, 5.1_

- [x] 6. Create DiscriminatorCodeGenerator for generating validation code
  - [x] 6.1 Implement GenerateMatchingCode method
    - Generate property existence check
    - Generate appropriate string comparison based on strategy (StartsWith, EndsWith, Contains, ExactMatch)
    - Handle null values appropriately
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 5.2, 7.2, 7.4_
  
  - [x] 6.2 Implement GenerateValueExtractionCode method
    - Generate code to extract discriminator value for error reporting
    - Handle missing properties gracefully
    - _Requirements: 5.2, 5.3_
  
  - [x] 6.3 Implement GenerateHelperMethods for projection classes
    - Generate MatchesDiscriminator static method
    - Generate GetDiscriminatorValue static method
    - _Requirements: 5.1, 5.2, 5.3_

- [x] 7. Update ProjectionExpressionGenerator
  - [x] 7.1 Include discriminator properties in projection expressions
    - Add entity-level discriminator property to attribute list
    - Add GSI-specific discriminator property if different
    - Avoid duplicating properties already in projection
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  
  - [x] 7.2 Generate discriminator validation in FromDynamoDb method
    - Call MatchesDiscriminator before property mapping
    - Throw DiscriminatorMismatchException on validation failure
    - Include expected and actual discriminator values in exception
    - Skip validation when no discriminator is configured
    - _Requirements: 5.1, 5.2, 5.3, 5.4_
  
  - [x] 7.3 Generate discriminator helper methods in projection classes
    - Use DiscriminatorCodeGenerator to create helper methods
    - Ensure methods are private and static
    - _Requirements: 5.1, 5.2, 7.2, 7.4_

- [x] 8. Add diagnostic warnings for configuration issues
  - [x]* 8.1 Detect and warn when both DiscriminatorValue and DiscriminatorPattern are specified
    - _Requirements: 8.1_
  
  - [x]* 8.2 Detect and error when DiscriminatorValue/Pattern specified without DiscriminatorProperty
    - _Requirements: 8.2_
  
  - [x]* 8.3 Validate pattern syntax and provide helpful error messages
    - _Requirements: 8.3, 8.4_

- [x] 9. Write unit tests for discriminator functionality
  - [ ]* 9.1 Test DiscriminatorAnalyzer pattern parsing
    - Test exact match patterns
    - Test StartsWith patterns (trailing wildcard)
    - Test EndsWith patterns (leading wildcard)
    - Test Contains patterns (wildcards at both ends)
    - Test complex patterns
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 7.1_
  
  - [ ]* 9.2 Test discriminator code generation
    - Verify generated matching code for each strategy
    - Verify generated value extraction code
    - Test null handling
    - _Requirements: 5.2, 5.3, 7.2, 7.4_
  
  - [ ]* 9.3 Test projection expression generation with discriminators
    - Verify discriminator properties are included
    - Verify no duplication of properties
    - Test GSI-specific discriminator inclusion
    - _Requirements: 6.1, 6.2, 6.3, 6.4_
  
  - [ ]* 9.4 Test backward compatibility
    - Verify EntityDiscriminator still works
    - Verify generated code is functionally equivalent
    - Test obsolescence warnings
    - _Requirements: 4.1, 4.2, 4.3, 4.4_

- [x] 10. Create integration tests with real DynamoDB scenarios
  - [x] 10.1 Test attribute-based discriminators
    - Query entities with entity_type discriminator
    - Verify validation works correctly
    - _Requirements: 1.1, 1.2, 5.1, 5.2_
  
  - [x] 10.2 Test sort key pattern discriminators
    - Query entities with SK prefix patterns
    - Test StartsWith, EndsWith, and Contains patterns
    - _Requirements: 1.4, 2.1, 2.2, 2.3_
  
  - [x] 10.3 Test GSI-specific discriminators
    - Query through GSI with different discriminator than primary key
    - Verify correct discriminator is used for validation
    - _Requirements: 3.1, 3.2, 3.3, 3.4_
  
  - [x] 10.4 Test multi-entity table queries
    - Query table containing multiple entity types
    - Verify correct filtering by discriminator
    - Test error handling for mismatched discriminators
    - _Requirements: 5.1, 5.2, 5.3_
