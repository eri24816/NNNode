from nodes import FunctionNode

class SumFunctionNode(FunctionNode):
    # Add two or more numbers.

    display_name = 'Σ'

    in_names = ["input"]
    out_names = ["sum"]
    allow_multiple_in_data = [True]

    frontend_type = 'RoundNode'

    @staticmethod
    def function(items):
        return sum(items)