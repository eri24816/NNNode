from nodes import FunctionNode

class SumFunctionNode(FunctionNode):
    # Add two or more numbers.

    display_name = 'Σ'

    in_names = ["input"]
    out_names = ["sum"]
    max_in_data = [64]

    frontend_type = 'RoundNode'

    @staticmethod
    def function(items):
        return sum(items)