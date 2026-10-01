@US06
Feature: Declare a career goal
  As a student
  I want to describe my goal in my own words
  So that the platform builds my learning path

  Background:
    Given a signed-in student "ana"

  Scenario: Declare an interpretable goal
    When "ana" declares the goal "quiero aprender a construir APIs REST con autenticación JWT"
    Then the response status is 201
    And the path contains the skills "networking-basics, programming-fundamentals, http-basics, rest-api-design, authentication-jwt" in this order
    And the available skills are "networking-basics, programming-fundamentals"

  Scenario: Reject a goal that matches no skill
    When "ana" declares the goal "quiero cocinar pasteles"
    Then the response status is 422
    And the error is "GoalNotInterpretable"
    And the error message is "We could not match your goal with any skill. Try describing it with more specific terms, such as a technology or a role."

  Scenario: Reject a blank goal
    When "ana" declares the goal "   "
    Then the response status is 400
    And the error is "InvalidGoal"

  Scenario: A student cannot have two active paths
    Given "ana" has declared the goal "quiero aprender SQL"
    When "ana" declares the goal "quiero aprender Docker"
    Then the response status is 409
    And the error is "ActivePathAlreadyExists"