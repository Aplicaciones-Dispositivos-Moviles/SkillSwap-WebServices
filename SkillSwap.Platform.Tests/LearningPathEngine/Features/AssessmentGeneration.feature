@US17
Feature: Generate the assessment of a node
  As a student
  I want to take a practical assessment of a skill of my path
  So that I can demonstrate that I acquired the knowledge, not only the document

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"

  Scenario: Generate the assessment of an available node
    When "ana" requests the assessment of the skill "networking-basics"
    Then the response status is 201
    And the blueprint has 5 questions with 4 answers each
    And the blueprint does not reveal the correct answers

  Scenario: Reject the assessment of a locked node
    When "ana" requests the assessment of the skill "rest-api-design"
    Then the response status is 409
    And the error is "NodeLocked"
    And the pending prerequisites are "http-basics, programming-fundamentals"

  Scenario: A new attempt generates different questions
    Given "ana" has requested the assessment of the skill "networking-basics"
    When "ana" requests the assessment of the skill "networking-basics"
    Then the response status is 201
    And the new questions differ from the previous ones

  Scenario: The node keeps its state when the AI service is unavailable
    Given the question generation service is unavailable
    When "ana" requests the assessment of the skill "networking-basics"
    Then the response status is 503
    And the error is "QuestionGenerationFailed"
    And "ana" sees no assessment for the skill "networking-basics"

  Scenario: A student cannot request the assessment of another student's node
    When "bob" requests the assessment of the skill "networking-basics" from the path of "ana"
    Then the response status is 403
    And the error is "NotPathOwner"