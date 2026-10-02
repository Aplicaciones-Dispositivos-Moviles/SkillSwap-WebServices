@TS11
Feature: Consult the reputation calculated by the platform
  As a student or a verifier
  I want to see the employability and the reliability that the platform calculated
  So that my demonstrated skills and my reviews are recognized without anyone rating me directly

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"

  Scenario: Passing an assessment certifies the skill automatically
    Given "ana" has completed the skill "networking-basics"
    When "ana" consults the employability of "ana"
    Then the response status is 200
    And the employability shows 1 verified skills and a score of 10

  Scenario: Certified skills add up
    Given "ana" has completed the skill "networking-basics"
    And "ana" has completed the skill "http-basics"
    When "ana" consults the employability of "ana"
    Then the response status is 200
    And the employability shows 2 verified skills and a score of 20

  Scenario: A student without certified skills has no employability yet
    When "ana" consults the employability of "ana"
    Then the response status is 404
    And the error is "ReputationNotFound"

  Scenario: A verifier's approval certifies the skill of the student
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    And "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    When "ana" consults the employability of "ana"
    Then the response status is 200
    And the employability shows 1 verified skills and a score of 10

  Scenario: A verifier's rejection does not certify the skill
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    And "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Rejected" with the notes "The explanation of the layers is incomplete."
    When "ana" consults the employability of "ana"
    Then the response status is 404
    And the error is "ReputationNotFound"

  Scenario: Resolving a case counts in the reliability of the verifier
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    And "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    When "bob" consults the reliability of "bob"
    Then the response status is 200
    And the reliability shows 1 resolved cases and a score of 100

  Scenario: The rating of the verifier profile follows the reliability
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    And "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Rejected" with the notes "The explanation of the layers is incomplete."
    When "bob" consults their verifier profile
    Then the response status is 200
    And the verifier profile has a rating of 100

  Scenario: A student cannot consult another student's employability
    Given "ana" has completed the skill "networking-basics"
    When "bob" consults the employability of "ana"
    Then the response status is 403
    And the error is "NotReputationOwner"

  Scenario: The reliability of a verifier is private
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    And "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    When "ana" consults the reliability of "bob"
    Then the response status is 403
    And the error is "NotReputationOwner"