Feature: Review the verification cases
  As a verifier
  I want to review the cases assigned to me following the rubric
  So that the students get a fair decision from a peer

  Background:
    Given a signed-in student "ana"
    And a signed-in student "bob"
    And a signed-in student "carl"
    And "ana" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has declared the goal "quiero aprender a construir APIs REST con autenticación JWT"
    And "bob" has completed the skill "networking-basics"
    And "bob" is a verifier of the skill "networking-basics"

  @US24
  Scenario: A failed attempt is assigned to an available verifier
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" consults the cases assigned to them
    Then the response status is 200
    And the number of assigned cases is 1
    And the case of "ana" is "Assigned" to "bob"

  @US24
  Scenario: The case waits when no verifier is available
    Given "bob" switches their availability off
    When "ana" has failed the assessment of the skill "networking-basics"
    Then the case of "ana" is "Pending"

  @US24
  Scenario: A pending case is assigned when the verifier becomes available
    Given "bob" switches their availability off
    And "ana" has failed the assessment of the skill "networking-basics"
    When "bob" switches their availability on
    Then the case of "ana" is "Assigned" to "bob"

  @US25
  Scenario: The verifier sees the failed questions without the correct answers
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" consults the case of "ana"
    Then the response status is 200
    And the case lists 5 failed questions
    And the case does not reveal the correct answers

  @US25
  Scenario: The verifier approves the case and the node is completed
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Approved" with the notes "Solid understanding of the network layers."
    Then the response status is 200
    And the case is resolved as "Approved" with the notes "Solid understanding of the network layers."
    And the skill "networking-basics" of "ana" is "Completed"

  @US25
  Scenario: The verifier rejects the case and the node stays available
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Rejected" with the notes "The explanation of the layers is incomplete."
    Then the response status is 200
    And the case is resolved as "Rejected" with the notes "The explanation of the layers is incomplete."
    And the skill "networking-basics" of "ana" is "Available"

  @US25
  Scenario: Only the assigned verifier can resolve the case
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "carl" resolves the case of "ana" as "Approved" with the notes "Looks fine."
    Then the response status is 403
    And the error is "NotAssignedVerifier"

  @US25
  Scenario: Reject a decision without notes
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Approved" with the notes ""
    Then the response status is 400
    And the error is "RubricNotesRequired"

  @US25
  Scenario: Reject an unknown decision
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" resolves the case of "ana" as "Maybe" with the notes "Not sure."
    Then the response status is 400
    And the error is "InvalidDecision"

  @US25
  Scenario: A resolved case cannot be resolved again
    Given "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Rejected" with the notes "Needs more work."
    When "bob" resolves the case of "ana" as "Approved" with the notes "Changed my mind."
    Then the response status is 409
    And the error is "CaseAlreadyResolved"

  @US25
  Scenario: A student who is not involved cannot consult the case
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "carl" consults the case of "ana"
    Then the response status is 403
    And the error is "NotCaseOwner"

  @US26
  Scenario: The student attaches evidence to the case
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "ana" attaches the evidence "https://github.com/ana/network-lab" to the case of "ana"
    Then the response status is 200
    And the case has the evidence "https://github.com/ana/network-lab"

  @US26
  Scenario: Reject evidence that is not a valid link
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "ana" attaches the evidence "not a link" to the case of "ana"
    Then the response status is 400
    And the error is "InvalidEvidenceUrl"

  @US26
  Scenario: Only the student can attach evidence to their case
    Given "ana" has failed the assessment of the skill "networking-basics"
    When "bob" attaches the evidence "https://github.com/bob/work" to the case of "ana"
    Then the response status is 403
    And the error is "NotCaseOwner"

  @US26
  Scenario: A resolved case does not accept evidence
    Given "ana" has failed the assessment of the skill "networking-basics"
    And "bob" has resolved the case of "ana" as "Rejected" with the notes "Needs more work."
    When "ana" attaches the evidence "https://github.com/ana/network-lab" to the case of "ana"
    Then the response status is 409
    And the error is "CaseAlreadyResolved"