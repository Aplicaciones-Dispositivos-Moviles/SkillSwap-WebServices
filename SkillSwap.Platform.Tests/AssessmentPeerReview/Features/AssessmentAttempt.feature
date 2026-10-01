@US18
Feature: Resolve the assessment with grading on the server
  As a student
  I want to answer the assessment of a skill of my path
  So that the platform grades it and recognizes what I demonstrated

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"

  Scenario: Approve the node with every answer correct
    Given "ana" has an assessment of the skill "networking-basics"
    When "ana" submits 5 correct answers for the assessment of the skill "networking-basics"
    Then the response status is 201
    And the attempt is approved with a score of 5 out of 5
    And the skill "networking-basics" of "ana" is "Completed"

  Scenario: Approve the node with exactly four correct answers
    Given "ana" has an assessment of the skill "networking-basics"
    When "ana" submits 4 correct answers for the assessment of the skill "networking-basics"
    Then the response status is 201
    And the attempt is approved with a score of 4 out of 5
    And the skill "networking-basics" of "ana" is "Completed"

  Scenario: A failed attempt opens a verification case
    Given "ana" has an assessment of the skill "networking-basics"
    When "ana" submits 3 correct answers for the assessment of the skill "networking-basics"
    Then the response status is 201
    And the attempt is not approved with a score of 3 out of 5
    And a verification case is opened for the attempt
    And the skill "networking-basics" of "ana" is "Available"

  Scenario: Reject an attempt with the wrong number of answers
    Given "ana" has an assessment of the skill "networking-basics"
    When "ana" submits the answers "1, 2, 3" for the assessment of the skill "networking-basics"
    Then the response status is 400
    And the error is "InvalidAnswers"

  Scenario: A student cannot answer another student's assessment
    Given "ana" has an assessment of the skill "networking-basics"
    When "bob" submits 5 correct answers for the assessment of the skill "networking-basics" from the path of "ana"
    Then the response status is 403
    And the error is "NotBlueprintOwner"

  Scenario: Reject an assessment that was replaced by a newer one
    Given "ana" has an assessment of the skill "networking-basics"
    And "ana" has an assessment of the skill "networking-basics"
    When "ana" submits 5 correct answers for the previous assessment of the skill "networking-basics"
    Then the response status is 409
    And the error is "BlueprintOutdated"

  Scenario: A new attempt is not accepted while the node has a case in progress
    Given "ana" has failed the assessment of the skill "networking-basics"
    And "ana" has an assessment of the skill "networking-basics"
    When "ana" submits 5 correct answers for the assessment of the skill "networking-basics"
    Then the response status is 409
    And the error is "OpenCaseAlreadyExists"

  Scenario: A student cannot consult another student's attempt
    Given "ana" has completed the skill "networking-basics"
    When "bob" consults the last attempt of "ana"
    Then the response status is 403
    And the error is "NotAttemptOwner"