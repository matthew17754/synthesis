#pragma once

#include <Core/Memory.h>

namespace BXDJ
{
	namespace Utility
	{
		///
		/// Gets the level of a Fusion occurrence in the document hierarchy.
		/// \param occurrence Occurrence to calculate the level of.
		/// \return The depth of the occurrence in the hierarchy. The root component is 0. nullptr will return -1.
		///
		int levelOfOccurrence(adsk::core::Ptr<adsk::fusion::Occurrence>);

		///
		/// Generates a unique identifier for a Fusion joint.
		/// \param joint Joint to create identifier for.
		/// \return Unique ID for the joint.
		///
		std::string getUniqueJointID(adsk::core::Ptr<adsk::fusion::Joint>);
	}
}
