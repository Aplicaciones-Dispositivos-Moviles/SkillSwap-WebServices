@US02
Feature: Sign in
  As a student
  I want to sign in with my credentials
  So that I can access my path, my certificates and my assessments

  Scenario: Sign in with valid credentials
    Given an account exists for "ana"
    When I sign in as "ana" with the password "password123"
    Then the response status is 200
    And the response contains an access token
    And the token grants access to the profile of "ana"

  Scenario: Sign in with an incorrect password
    Given an account exists for "ana"
    When I sign in as "ana" with the password "wrong-password"
    Then the response status is 401
    And the error is "InvalidCredentials"

  Scenario: An unknown username gets the same error as a wrong password
    When I sign in as "nobody" with the password "password123"
    Then the response status is 401
    And the error is "InvalidCredentials"
    And the error message is "Invalid username or password."

  Scenario: Error messages follow the language of the client
    Given the client prefers the language "es-PE"
    When I sign in as "nobody" with the password "password123"
    Then the error message is "Usuario o contraseña incorrectos."