Feature: Enable students as verifiers
  As a student who demonstrated a skill
  I want to review the cases of other students in that skill
  So that the peer review has enough verifiers

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"

  @US22
  Scenario: A student who completed a skill becomes a verifier of it
    Given "bob" has completed the skill "networking-basics"
    When "bob" creates a verifier profile for the skill "networking-basics"
    Then the response status is 201
    And the verifier profile has the skills "networking-basics"
    And the verifier profile is available

  @US22
  Scenario: A verifier adds a second skill to the profile
    Given "bob" has completed the skill "networking-basics"
    And "bob" has completed the skill "http-basics"
    And "bob" is a verifier of the skill "networking-basics"
    When "bob" creates a verifier profile for the skill "http-basics"
    Then the response status is 200
    And the verifier profile has the skills "http-basics, networking-basics"

  @US22
  Scenario: Reject a skill that the student did not complete
    When "bob" creates a verifier profile for the skill "networking-basics"
    Then the response status is 409
    And the error is "SkillNotCompleted"

  @US22
  Scenario: Reject a skill the verifier already has
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    When "bob" creates a verifier profile for the skill "networking-basics"
    Then the response status is 409
    And the error is "VerifierSkillAlreadyEnabled"

  @US23
  Scenario: A verifier switches their availability off
    Given "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"
    When "bob" switches their availability off
    Then the response status is 200
    And the verifier profile is not available

  @US23
  Scenario: A student who is not a verifier cannot change the availability
    When "ana" switches their availability off
    Then the response status is 403
    And the error is "NotAVerifier"