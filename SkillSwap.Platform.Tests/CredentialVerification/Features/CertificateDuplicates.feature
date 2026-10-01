@US14
Feature: Duplicate certificate detection
  As a student
  I want the platform to detect repeated and reused certificates
  So that every certificate counts as reliable evidence

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"

  Scenario: Register an original certificate
    When "ana" uploads the certificate file "original"
    Then the response status is 201
    And the certificate status is "Unverified"
    And the risk level is "LowRisk"

  Scenario: Reject a file the same student already registered
       Given "ana" has uploaded the certificate file "original"
       When "ana" uploads the certificate file "original"
       Then the response status is 409
       And the error is "DuplicateFile"
       And the error references the existing certificate of "ana"

  Scenario: A certificate number registered by another student requires review
    Given "ana" has uploaded the certificate file "ana-file" with the number "CERT-001" and the code "CODE-A"
    When "bob" uploads the certificate file "bob-file" with the number "CERT-001" and the code "CODE-B"
    Then the response status is 201
    And the certificate status is "Unverified"
    And the risk level is "Review"

  Scenario: A number and a code registered by another student flag the certificate as suspicious
    Given "ana" has uploaded the certificate file "ana-file" with the number "CERT-001" and the code "CODE-A"
    When "bob" uploads the certificate file "bob-file" with the number "cert-001" and the code "code-a"
    Then the response status is 201
    And the certificate status is "Suspicious"
    And the risk level is "HighRisk"
    
  Scenario: A file already registered by another student flags the certificate as suspicious
    Given "ana" has uploaded the certificate file "shared-file"
    When "bob" uploads the certificate file "shared-file"
    Then the response status is 201
    And the certificate status is "Suspicious"
    And the risk level is "HighRisk"