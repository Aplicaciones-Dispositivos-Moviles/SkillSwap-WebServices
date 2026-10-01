@US08
Feature: Consult the learning path
  As a student
  I want to consult my learning path with the state of each skill
  So that I know what I have completed and what my next step is

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"

  Scenario: A student consults their active path
    Given "ana" has declared the goal "quiero aprender HTTP"
    When "ana" consults the learning path of "ana"
    Then the response status is 200
    And the path contains the skills "networking-basics, http-basics" in this order
    And the available skills are "networking-basics"

  Scenario: A student without a path gets not found
    When "ana" consults the learning path of "ana"
    Then the response status is 404
    And the error is "PathNotFound"

  Scenario: A student cannot consult another student's path
    Given "ana" has declared the goal "quiero aprender HTTP"
    When "bob" consults the learning path of "ana"
    Then the response status is 403
    And the error is "NotPathOwner"