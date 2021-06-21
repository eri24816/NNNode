from nodes import FunctionNode

class AddFunctionNode(FunctionNode):
    # Add two or more numbers.

    in_names = ["input"]
    out_names = ["sum"]
    allow_multiple_in_data = [True]

    @staticmethod
    def function(items):
        return sum(items)