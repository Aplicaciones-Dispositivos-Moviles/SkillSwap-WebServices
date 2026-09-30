@US04
Feature: Student profile
  As a student
  I want to keep a description in my profile
  So that the platform can personalize my experience

  Scenario: A student updates their own bio
    Given a signed-in student "ana"
    When "ana" updates the bio of "ana" to "Backend developer"
    Then the response status is 200
    And the bio of "ana" is "Backend developer"

  Scenario: A student cannot update another student's bio
    Given a signed-in student "ana"
    And a signed-in student "bob"
    When "bob" updates the bio of "ana" to "Hacked"
    Then the response status is 403
    And the bio of "ana" is empty

  Scenario: Another student's email stays private
    Given a signed-in student "ana"
    And a signed-in student "bob"
    When "bob" requests the profile of "ana"
    Then the response status is 200
    And the profile exposes the username "ana"
    And the profile does not expose the email