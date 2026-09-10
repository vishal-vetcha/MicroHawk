import json
from pathlib import Path
import pytest
from pydantic import TypeAdapter, ValidationError
from microhawk.contracts.wire import CommandEnvelope

@pytest.mark.parametrize("path", sorted((Path(__file__).resolve().parents[3]/"contracts/fixtures/v1").glob("*.json")))
def test_shared_command_fixture(path):
    adapter=TypeAdapter(CommandEnvelope)
    if path.name.startswith("valid_"): adapter.validate_json(path.read_text())
    else:
        with pytest.raises(ValidationError): adapter.validate_json(path.read_text())
